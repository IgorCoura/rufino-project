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
internal sealed record AsaasFailure(string ReasonCode, string? Message, bool IsRetryable);

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
            404 => new AsaasFailure(code ?? "not_found", message, IsRetryable: false),
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

    public static decimal? ReadDecimal(string? raw)
        => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;

    public static Money? ReadMoney(string? raw)
    {
        var value = ReadDecimal(raw);
        return value is null ? null : new Money(value.Value, Currency.BRL);
    }

    public static DateOnly? ReadDate(string? raw)
        => DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value) ? value : null;

    public static DateTimeOffset? ReadTimestamp(string? raw)
        => DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value)
            ? value
            : null;

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
