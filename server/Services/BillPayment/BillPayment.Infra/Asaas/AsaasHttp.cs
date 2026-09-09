namespace BillPayment.Infra.Asaas;

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

/// <summary>
/// Motivo pelo qual uma chamada ao provedor não produziu corpo.
/// </summary>
/// <param name="IsRetryable">
/// Separa "o provedor não sabe deste documento" de "o provedor não respondeu". É esta
/// distinção que decide entre <c>Unresolved</c> e <c>Unavailable</c> — e ela existe porque
/// tratar indisponibilidade de rede como suspeita do boleto seria bloquear pagamento legítimo.
/// </param>
/// <param name="IsNotFound">
/// Só o 404 do provedor. A consulta por id precisa distinguir "ele não conhece esta ordem"
/// (NotFound legítimo) de qualquer outra recusa — que degrada para indisponível, o lado seguro.
/// </param>
internal sealed record AsaasFailure(string ReasonCode, string? Message, bool IsRetryable, bool IsNotFound = false);

/// <summary>
/// A chamada HTTP crua ao provedor, com a tradução de status e exceção em motivo estável.
/// </summary>
/// <remarks>
/// <strong>Nada aqui loga linha digitável, BR Code ou chave de API.</strong> São instrumentos de
/// pagamento e credencial; o log carrega só o caminho, o status e o código do motivo.
/// </remarks>
internal static class AsaasHttp
{
    public const string LOOKUP_CLIENT_NAME = "asaas-lookup";

    /// <summary>
    /// O cliente de pagamento é OUTRO, e a diferença é a resiliência: este <strong>não
    /// retenta</strong>. Uma retentativa automática numa submissão é candidata a pagamento
    /// duplicado — a retentativa é da fila, e começa conferindo por <c>externalReference</c>.
    /// </summary>
    public const string PAYMENT_CLIENT_NAME = "asaas-payment";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<(TResponse? Body, AsaasFailure? Failure)> PostAsync<TResponse>(
        this HttpClient http,
        string path,
        object payload,
        ILogger logger,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        try
        {
            using var response = await http.PostAsJsonAsync(path, payload, Json, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var parsed = JsonSerializer.Deserialize<TResponse>(content, Json);
                return parsed is null
                    ? (null, new AsaasFailure("empty_response", null, IsRetryable: false))
                    : (parsed, null);
            }

            var failure = Classify((int)response.StatusCode, content);
            logger.LogWarning(
                "Consulta ao Asaas em {Path} respondeu {Status}: {ReasonCode}",
                path, (int)response.StatusCode, failure.ReasonCode);

            return (null, failure);
        }
        catch (JsonException)
        {
            // Corpo que não é o contrato esperado é fato sobre a resposta, não sobre a rede:
            // retentar devolveria o mesmo lixo.
            return (null, new AsaasFailure("malformed_response", null, IsRetryable: false));
        }
        catch (Exception ex) when (IsTransport(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Consulta ao Asaas em {Path} não obteve resposta", path);
            return (null, new AsaasFailure(TransportReason(ex), ex.Message, IsRetryable: true));
        }
    }

    public static async Task<(TResponse? Body, AsaasFailure? Failure)> GetAsync<TResponse>(
        this HttpClient http,
        string path,
        ILogger logger,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        try
        {
            using var response = await http.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var parsed = JsonSerializer.Deserialize<TResponse>(content, Json);
                return parsed is null
                    ? (null, new AsaasFailure("empty_response", null, IsRetryable: false))
                    : (parsed, null);
            }

            var failure = Classify((int)response.StatusCode, content);
            logger.LogWarning(
                "Consulta ao Asaas em {Path} respondeu {Status}: {ReasonCode}",
                path, (int)response.StatusCode, failure.ReasonCode);

            return (null, failure);
        }
        catch (JsonException)
        {
            return (null, new AsaasFailure("malformed_response", null, IsRetryable: false));
        }
        catch (Exception ex) when (IsTransport(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Consulta ao Asaas em {Path} não obteve resposta", path);
            return (null, new AsaasFailure(TransportReason(ex), ex.Message, IsRetryable: true));
        }
    }

    /// <summary>
    /// Atualização. Mesma classificação de falha do POST — só o verbo muda.
    /// </summary>
    public static async Task<(TResponse? Body, AsaasFailure? Failure)> PutAsync<TResponse>(
        this HttpClient http,
        string path,
        object payload,
        ILogger logger,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        try
        {
            using var response = await http.PutAsJsonAsync(path, payload, Json, cancellationToken);
            return await ReadAsync<TResponse>(response, path, logger, cancellationToken);
        }
        catch (JsonException)
        {
            return (null, new AsaasFailure("malformed_response", null, IsRetryable: false));
        }
        catch (Exception ex) when (IsTransport(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Consulta ao Asaas em {Path} não obteve resposta", path);
            return (null, new AsaasFailure(TransportReason(ex), ex.Message, IsRetryable: true));
        }
    }

    public static async Task<(TResponse? Body, AsaasFailure? Failure)> DeleteAsync<TResponse>(
        this HttpClient http,
        string path,
        ILogger logger,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        try
        {
            using var response = await http.DeleteAsync(new Uri(path, UriKind.Relative), cancellationToken);
            return await ReadAsync<TResponse>(response, path, logger, cancellationToken);
        }
        catch (JsonException)
        {
            return (null, new AsaasFailure("malformed_response", null, IsRetryable: false));
        }
        catch (Exception ex) when (IsTransport(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Consulta ao Asaas em {Path} não obteve resposta", path);
            return (null, new AsaasFailure(TransportReason(ex), ex.Message, IsRetryable: true));
        }
    }

    /// <summary>Leitura e classificação comuns aos verbos novos.</summary>
    private static async Task<(TResponse? Body, AsaasFailure? Failure)> ReadAsync<TResponse>(
        HttpResponseMessage response,
        string path,
        ILogger logger,
        CancellationToken cancellationToken)
        where TResponse : class
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var parsed = JsonSerializer.Deserialize<TResponse>(content, Json);
            return parsed is null
                ? (null, new AsaasFailure("empty_response", null, IsRetryable: false))
                : (parsed, null);
        }

        var failure = Classify((int)response.StatusCode, content);
        logger.LogWarning(
            "Consulta ao Asaas em {Path} respondeu {Status}: {ReasonCode}",
            path, (int)response.StatusCode, failure.ReasonCode);

        return (null, failure);
    }

    /// <summary>
    /// O 4xx genérico cai em não-retentável de propósito: é resposta do provedor sobre o que
    /// foi enviado. Só permissão, limite de taxa e falha do lado dele são retentáveis.
    /// </summary>
    private static AsaasFailure Classify(int status, string content)
    {
        var error = TryReadFirstError(content);
        var code = string.IsNullOrWhiteSpace(error?.Code) ? null : error!.Code;
        var message = string.IsNullOrWhiteSpace(error?.Description) ? null : error!.Description;

        return status switch
        {
            401 or 403 => new AsaasFailure(code ?? "insufficient_permission", message, IsRetryable: true),
            404 => new AsaasFailure(code ?? "not_found", message, IsRetryable: false, IsNotFound: true),
            408 or 429 => new AsaasFailure(code ?? "rate_limited", message, IsRetryable: true),
            >= 500 => new AsaasFailure(code ?? "provider_error", message, IsRetryable: true),
            _ => new AsaasFailure(
                code ?? string.Create(CultureInfo.InvariantCulture, $"http_{status}"),
                message,
                IsRetryable: false),
        };
    }

    private static AsaasError? TryReadFirstError(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            return JsonSerializer.Deserialize<AsaasErrorResponse>(content, Json)?.Errors?.FirstOrDefault();
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
    /// O fuso em que o provedor responde. <strong>Constante do integrador, não do servidor</strong>
    /// — em contêiner o fuso da máquina é quase sempre UTC, e usá-lo deslocaria toda leitura.
    /// </summary>
    /// <remarks>
    /// Mesmo par IANA/Windows do <c>PaymentSchedulingOptions</c>: o id IANA funciona nos dois
    /// sistemas desde o .NET 6 (ICU), e o fallback cobre a máquina Windows sem ICU.
    /// </remarks>
    private static readonly TimeZoneInfo ProviderTimeZone = ResolveProviderTimeZone();

    private static TimeZoneInfo ResolveProviderTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
    }

    public static decimal? ReadDecimal(string? raw)
        => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;

    public static Money? ReadMoney(string? raw)
    {
        var value = ReadDecimal(raw);
        return value is null ? null : new Money(value.Value, Currency.BRL);
    }

    /// <summary>
    /// A data que o provedor afirmou, aceitando também os campos que vêm com hora.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>O provedor serializa data-hora como <c>"yyyy-MM-dd HH:mm:ss"</c>, e
    /// <c>DateOnly.TryParse</c> RECUSA esse formato</strong> — medido em 2026-09-09. Só isso já
    /// custou um pagamento: o <c>effectiveDate</c> de uma transação Pix <c>DONE</c> virava
    /// <c>null</c>, e "pago sem data de pagamento" é <c>BLP.PMO03</c>, que derrubava a
    /// conciliação daquela ordem a cada ciclo, para sempre. O defeito é invisível porque a falha
    /// de leitura não é exceção: é um <c>null</c> que só vira problema três camadas adiante.
    /// </para>
    /// <para>
    /// <strong>A parte da data é tomada SEM converter fuso</strong>, e isso é deliberado: o
    /// provedor responde em horário de Brasília sem deslocamento explícito, e normalizar para UTC
    /// empurraria para o dia seguinte todo pagamento efetivado depois das 21h. A data que vale é a
    /// que ele afirmou. Quando o texto TRAZ deslocamento, ele é respeitado e a data é a do próprio
    /// instante informado.
    /// </para>
    /// </remarks>
    public static DateOnly? ReadDate(string? raw)
    {
        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;

        return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var moment)
            ? DateOnly.FromDateTime(moment)
            : null;
    }

    /// <summary>
    /// Um instante do provedor. Texto sem deslocamento é lido como <strong>hora de Brasília</strong>.
    /// </summary>
    /// <remarks>
    /// <c>AssumeUniversal</c> estava errado pelo mesmo motivo do <see cref="ReadDate"/>: o provedor
    /// responde no fuso dele, então tratar <c>"23:59:59"</c> como UTC dava a um QR Pix uma
    /// expiração <strong>três horas mais cedo</strong> que a real.
    /// </remarks>
    public static DateTimeOffset? ReadTimestamp(string? raw)
    {
        // AssumeLocal seria o fuso da MÁQUINA — em contêiner, quase sempre UTC. O fuso do
        // provedor é uma constante do integrador, não do servidor onde isto roda.
        if (!DateTimeOffset.TryParse(
                raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var withOffset))
        {
            return null;
        }

        if (withOffset.Offset != TimeSpan.Zero || HasExplicitOffset(raw))
            return withOffset;

        var unspecified = DateTime.SpecifyKind(withOffset.DateTime, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, ProviderTimeZone.GetUtcOffset(unspecified));
    }

    /// <summary>
    /// O texto declara fuso? Sem isso, um instante que por acaso caia num deslocamento zero
    /// seria confundido com um instante sem fuso e deslocado de novo.
    /// </summary>
    private static bool HasExplicitOffset(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var trimmed = raw.TrimEnd();
        if (trimmed.EndsWith('Z') || trimmed.EndsWith('z'))
            return true;

        // Um '+' ou '-' depois da parte da hora — nunca o hífen das datas, que vem antes dela.
        var timeAt = trimmed.IndexOf(':', StringComparison.Ordinal);
        return timeAt >= 0 && trimmed.IndexOfAny(['+', '-'], timeAt) >= 0;
    }

    /// <summary>
    /// O código do banco aceitando as duas formas plausíveis, porque a resposta preenchida
    /// nunca foi observada (0% do corpus) — ver o comentário em <see cref="AsaasBankSlipInfo.Bank"/>.
    /// </summary>
    public static BankCode? ReadBankCode(JsonElement? element)
    {
        if (element is not { } value)
            return null;

        var raw = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.Object when value.TryGetProperty("code", out var code) => code.ValueKind == JsonValueKind.Number
                ? code.GetRawText()
                : code.GetString(),
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var digits = new string(raw.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length is 0 or > BankCode.LENGTH)
            return null;

        // Código não atribuído ("000") é recusado pelo VO; aqui vira ausência, não exceção.
        var padded = digits.PadLeft(BankCode.LENGTH, '0');
        return padded == "000" ? null : new BankCode(padded);
    }
}
