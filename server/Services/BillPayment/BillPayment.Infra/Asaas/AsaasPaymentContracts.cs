namespace BillPayment.Infra.Asaas;

using System.Text.Json;
using System.Text.Json.Serialization;
using BillPayment.Domain.PaymentOrders;

// DTOs do pague-contas e do pagamento Pix. Mesma doutrina dos contratos de consulta: nenhum
// cruza a fronteira da Infra, valores e datas são string frouxa, e o contrato é MEDIDO — o que
// a sonda de sandbox ainda não provou está anotado no campo.

internal sealed class AsaasBillPaymentResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("value")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Value { get; set; }

    [JsonPropertyName("fee")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Fee { get; set; }

    [JsonPropertyName("scheduleDate")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? ScheduleDate { get; set; }

    [JsonPropertyName("paymentDate")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? PaymentDate { get; set; }

    [JsonPropertyName("transactionReceiptUrl")]
    public string? TransactionReceiptUrl { get; set; }

    [JsonPropertyName("canBeCancelled")]
    public bool? CanBeCancelled { get; set; }

    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; }

    /// <summary>
    /// A documentação diz "array" sem dizer de quê. Lido cru para aceitar strings ou objetos —
    /// adivinhar errado derrubaria a resposta inteira num campo que é só diagnóstico.
    /// </summary>
    [JsonPropertyName("failReasons")]
    public JsonElement? FailReasons { get; set; }
}

internal sealed class AsaasBillPaymentListResponse
{
    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; set; }

    [JsonPropertyName("data")]
    public List<AsaasBillPaymentResponse>? Data { get; set; }
}

internal sealed class AsaasPixPaymentResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("value")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? Value { get; set; }

    [JsonPropertyName("chargedFeeValue")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? ChargedFeeValue { get; set; }

    [JsonPropertyName("scheduledDate")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? ScheduledDate { get; set; }

    [JsonPropertyName("effectiveDate")]
    [JsonConverter(typeof(LenientStringConverter))]
    public string? EffectiveDate { get; set; }

    [JsonPropertyName("endToEndIdentifier")]
    public string? EndToEndIdentifier { get; set; }

    [JsonPropertyName("transactionReceiptUrl")]
    public string? TransactionReceiptUrl { get; set; }

    [JsonPropertyName("canBeCanceled")]
    public bool? CanBeCanceled { get; set; }

    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; }

    [JsonPropertyName("refusalReason")]
    public string? RefusalReason { get; set; }

    /// <summary>
    /// MEDIDO (2026-09-08): o provedor DESCARTA o <c>externalReference</c> deste endpoint, mas
    /// grava e devolve o <c>description</c>. É por ele que a busca de idempotência casa a
    /// transação — ver <c>AsaasPixPaymentGateway.BuildReferenceMarker</c>.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// O <c>transfer</c> espelho desta transação. É o id que chega nos webhooks
    /// <c>TRANSFER_*</c> — a única família de eventos que o provedor emite para saída de Pix.
    /// </summary>
    [JsonPropertyName("transferId")]
    public string? TransferId { get; set; }
}

internal sealed class AsaasPixPaymentListResponse
{
    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; set; }

    [JsonPropertyName("data")]
    public List<AsaasPixPaymentResponse>? Data { get; set; }
}

/// <summary>
/// Monta o retrato (<c>ProviderPaymentSnapshot</c>) a partir dos DTOs do provedor. A tradução de
/// status vive no <see cref="ProviderStatusCatalog"/> do Domain — mapa único, consultado também
/// pelo webhook; os dois delegam para nunca mais discordarem em silêncio.
/// </summary>
internal static class AsaasPaymentStatusMap
{
    /// <summary>O status que o provedor usa enquanto a autorização de ação crítica não chega.</summary>
    private const string AWAITING_CRITICAL_ACTION_AUTHORIZATION = "AWAITING_CRITICAL_ACTION_AUTHORIZATION";

    public static PaymentOrderStatus FromBillPayment(string? raw)
        => ProviderStatusCatalog.FromBillPayment(raw);

    public static PaymentOrderStatus FromPixPayment(string? raw)
        => ProviderStatusCatalog.FromPixPayment(raw);

    public static IReadOnlyCollection<string> ReadFailReasons(JsonElement? element)
    {
        if (element is not { ValueKind: JsonValueKind.Array } array)
            return [];

        var reasons = new List<string>();
        foreach (var item in array.EnumerateArray())
        {
            var reason = item.ValueKind switch
            {
                JsonValueKind.String => item.GetString(),
                JsonValueKind.Object when item.TryGetProperty("description", out var description)
                    => description.GetString(),
                _ => item.GetRawText(),
            };

            if (!string.IsNullOrWhiteSpace(reason))
                reasons.Add(reason);
        }

        return reasons;
    }

    public static ProviderPaymentSnapshot ToSnapshot(AsaasBillPaymentResponse body)
        => new(
            body.Id ?? string.Empty,
            FromBillPayment(body.Status),
            body.Status ?? "unknown",
            AsaasHttp.ReadDate(body.ScheduleDate),
            AsaasHttp.ReadDate(body.PaymentDate),
            AsaasHttp.ReadMoney(body.Fee),
            ReadFailReasons(body.FailReasons),
            body.TransactionReceiptUrl,
            AsaasHttp.ReadMoney(body.Value));

    public static ProviderPaymentSnapshot ToSnapshot(AsaasPixPaymentResponse body)
        => new(
            body.Id ?? string.Empty,
            FromPixPayment(body.Status),
            body.Status ?? "unknown",
            AsaasHttp.ReadDate(body.ScheduledDate),
            AsaasHttp.ReadDate(body.EffectiveDate),
            AsaasHttp.ReadMoney(body.ChargedFeeValue),
            string.IsNullOrWhiteSpace(body.RefusalReason) ? [] : [body.RefusalReason],
            body.TransactionReceiptUrl,
            AsaasHttp.ReadMoney(body.Value),
            body.TransferId,
            IsAuthorized(body.Status));

    /// <summary>
    /// Se o provedor já liberou a ação crítica. Derivado do status cru porque o endpoint de
    /// transação Pix não traz o booleano <c>authorized</c> que o <c>transfer</c> traz —
    /// <c>AWAITING_CRITICAL_ACTION_AUTHORIZATION</c> é exatamente "esperando alguém digitar o
    /// código no celular" (medido em sandbox, 2026-09-08).
    /// </summary>
    private static bool? IsAuthorized(string? rawStatus)
        => string.IsNullOrWhiteSpace(rawStatus)
            ? null
            : !string.Equals(
                rawStatus.Trim(),
                AWAITING_CRITICAL_ACTION_AUTHORIZATION,
                StringComparison.OrdinalIgnoreCase);
}
