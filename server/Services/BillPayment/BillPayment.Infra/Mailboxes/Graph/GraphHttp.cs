namespace BillPayment.Infra.Mailboxes.Graph;

using System.Globalization;
using System.Text.Json;
using BillPayment.Domain.Mailboxes;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

/// <summary>
/// Motivo pelo qual uma chamada ao Graph não produziu corpo, já traduzido para o desfecho que o
/// domínio entende.
/// </summary>
/// <param name="Status">
/// A tradução que importa. <c>Denied</c> é fato sobre a <strong>autorização</strong> e não
/// melhora com retentativa; <c>CursorExpired</c> pede varredura completa; <c>Unavailable</c> é
/// fato sobre a infraestrutura. Colapsá-los faria a fonte parar de sincronizar em silêncio ou
/// pedir reconfiguração a quem só precisava esperar.
/// </param>
internal sealed record GraphFailure(MailboxStatus Status, string ReasonCode, string? Message);

/// <summary>
/// A chamada HTTP crua ao Graph, com a tradução de status e exceção em desfecho estável.
/// </summary>
/// <remarks>
/// <strong>Nada aqui loga segredo, token ou conteúdo de mensagem.</strong> O log carrega o
/// caminho, o status e o código do motivo — assunto e remetente já são dado de cliente e não
/// entram nem em diagnóstico.
/// </remarks>
internal static class GraphHttp
{
    public const string CLIENT_NAME = "graph-mailbox";
    public const string TOKEN_CLIENT_NAME = "graph-token";

    /// <summary>
    /// Pede ao Graph os identificadores <strong>imutáveis</strong> dos itens do Outlook.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sem isto, o id de uma mensagem é o endereço de onde ela está guardada, e a pasta faz parte
    /// dele: arquivar, mover por regra ou mandar para a lixeira invalida o id, e o download do
    /// anexo passa a devolver 404 para sempre. Medido em produção em 2026-08-19 — 2.381 downloads
    /// com êxito e 6 falhas, todas com esse formato.
    /// </para>
    /// <para>
    /// O cabeçalho vale <strong>por requisição</strong> e cobre mensagem e anexo. A delta query o
    /// honra, e os <c>@odata.nextLink</c>/<c>@odata.deltaLink</c> são compatíveis com os dois
    /// formatos — ligar não obriga a reler caixa nenhuma. A limitação conhecida é com
    /// <c>$search</c>, que este adapter não usa.
    /// </para>
    /// </remarks>
    public const string IMMUTABLE_ID_PREFER = "IdType=\"ImmutableId\"";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <param name="maxPageSize">
    /// Vira o header <c>Prefer: odata.maxpagesize</c>. <strong>A delta query ignora
    /// <c>$top</c></strong> — medido em 2026-08-11: um pedido com <c>$top=50</c> voltou com 10
    /// mensagens. O tamanho de página só é respeitado por este header.
    /// </param>
    public static async Task<(TResponse? Body, GraphFailure? Failure)> GetAsync<TResponse>(
        this HttpClient http,
        string url,
        string accessToken,
        ILogger logger,
        CancellationToken cancellationToken,
        int? maxPageSize = null)
        where TResponse : class
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.TryAddWithoutValidation("Prefer", IMMUTABLE_ID_PREFER);

            if (maxPageSize is > 0)
            {
                request.Headers.TryAddWithoutValidation(
                    "Prefer",
                    string.Create(CultureInfo.InvariantCulture, $"odata.maxpagesize={maxPageSize}"));
            }

            using var response = await http.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var parsed = JsonSerializer.Deserialize<TResponse>(content, Json);
                return parsed is null
                    ? (null, new GraphFailure(MailboxStatus.Unavailable, "empty_response", null))
                    : (parsed, null);
            }

            var failure = Classify((int)response.StatusCode, content);
            logger.LogWarning(
                "Graph respondeu {Status} em {Path}: {ReasonCode}",
                (int)response.StatusCode, Redact(url), failure.ReasonCode);

            return (null, failure);
        }
        catch (JsonException)
        {
            // Corpo fora do contrato é fato sobre a resposta, não sobre a rede — mas continua
            // sendo indisponibilidade do ponto de vista da fonte: nada foi lido.
            return (null, new GraphFailure(MailboxStatus.Unavailable, "malformed_response", null));
        }
        catch (Exception ex) when (IsTransport(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Graph não respondeu em {Path}", Redact(url));
            return (null, new GraphFailure(MailboxStatus.Unavailable, TransportReason(ex), ex.Message));
        }
    }

    /// <summary>
    /// Baixa bytes crus — o conteúdo de um anexo, não JSON.
    /// </summary>
    /// <remarks>
    /// Devolve <c>null</c> em qualquer falha, de propósito: um anexo que não veio não pode
    /// derrubar o processamento dos outros, e o desfecho já é registrado no próprio item.
    /// </remarks>
    public static async Task<ReadOnlyMemory<byte>?> GetBytesAsync(
        this HttpClient http,
        string url,
        string accessToken,
        long maxBytes,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.TryAddWithoutValidation("Prefer", IMMUTABLE_ID_PREFER);

            using var response = await http.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Graph recusou o download de um anexo: {Status}", (int)response.StatusCode);
                return null;
            }

            // O teto vem do cabeçalho quando o provedor o informa — recusar antes de ler evita
            // trazer um arquivo grande para a memória só para descartá-lo depois.
            if (response.Content.Headers.ContentLength is > 0 and var declared && declared > maxBytes)
            {
                logger.LogWarning("Anexo acima do teto configurado; download recusado.");
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            // E de novo depois de ler, porque o cabeçalho pode vir ausente ou mentindo.
            return bytes.LongLength > maxBytes ? null : bytes;
        }
        catch (Exception ex) when (IsTransport(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Graph não respondeu ao download de um anexo.");
            return null;
        }
    }

    /// <summary>
    /// Traduz o status HTTP no desfecho de domínio.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>401 e 403 são <c>Denied</c>, não <c>Unavailable</c></strong> — e essa é a
    /// diferença mais importante daqui. No Asaas, 403 é retentável porque costuma ser limite de
    /// taxa disfarçado; no Graph é a Application Access Policy dizendo que aquele aplicativo não
    /// alcança aquela caixa. Retentar a cada minuto para sempre esconderia o problema de
    /// configuração que só uma pessoa resolve.
    /// </para>
    /// <para>
    /// <strong>410 é <c>CursorExpired</c></strong>: o Graph invalida o <c>deltaLink</c> velho com
    /// <c>resyncRequired</c>. A resposta é descartar o cursor e varrer tudo, não retentar igual.
    /// </para>
    /// </remarks>
    private static GraphFailure Classify(int status, string content)
    {
        var error = TryReadError(content);
        var code = string.IsNullOrWhiteSpace(error?.Code) ? null : error!.Code;
        var message = string.IsNullOrWhiteSpace(error?.Message) ? null : error!.Message;

        return status switch
        {
            401 => new GraphFailure(MailboxStatus.Denied, code ?? "unauthenticated", message),
            403 => new GraphFailure(MailboxStatus.Denied, code ?? "insufficient_permission", message),
            404 => new GraphFailure(MailboxStatus.Denied, code ?? "mailbox_not_found", message),
            410 => new GraphFailure(MailboxStatus.CursorExpired, code ?? "delta_token_expired", message),
            408 or 429 => new GraphFailure(MailboxStatus.Unavailable, code ?? "throttled", message),
            >= 500 => new GraphFailure(MailboxStatus.Unavailable, code ?? "provider_error", message),

            // 4xx genérico é resposta sobre o que foi enviado — defeito nosso, não da caixa.
            // Fica em Denied para aparecer como algo a corrigir, e não como espera indefinida.
            _ => new GraphFailure(
                MailboxStatus.Denied,
                code ?? string.Create(CultureInfo.InvariantCulture, $"http_{status}"),
                message),
        };
    }

    private static GraphError? TryReadError(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            return JsonSerializer.Deserialize<GraphErrorResponse>(content, Json)?.Error;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Cancelamento pedido por quem chamou não é falha do provedor e tem de subir.
    private static bool IsTransport(Exception ex, CancellationToken cancellationToken)
        => ex switch
        {
            OperationCanceledException => !cancellationToken.IsCancellationRequested,
            HttpRequestException or TimeoutRejectedException or BrokenCircuitException => true,
            _ => false,
        };

    private static string TransportReason(Exception ex)
        => ex switch
        {
            BrokenCircuitException => "circuit_open",
            TimeoutRejectedException or OperationCanceledException => "timeout",
            _ => "transport_error",
        };

    /// <summary>
    /// O <c>deltaLink</c> carrega um token opaco na query string, e o caminho carrega o endereço
    /// da caixa. Nenhum dos dois entra em log.
    /// </summary>
    private static string Redact(string url)
    {
        var withoutQuery = url.Split('?', 2)[0];
        var at = withoutQuery.IndexOf('@', StringComparison.Ordinal);
        return at < 0 ? withoutQuery : "(caixa)";
    }
}
