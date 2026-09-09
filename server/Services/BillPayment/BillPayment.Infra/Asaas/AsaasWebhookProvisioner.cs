namespace BillPayment.Infra.Asaas;

using System.Security.Cryptography;
using System.Text.Json.Serialization;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provisiona o webhook da conta do tenant em <c>POST /v3/webhooks</c> e vizinhos.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Contrato MEDIDO em sandbox (2026-09-08)</strong>, não lido da documentação — regra
/// deste BC. O que a medição fixou e o código depende:
/// </para>
/// <list type="bullet">
/// <item>o <c>authToken</c> exige <strong>ao menos 32 caracteres</strong> (o provedor recusa com
/// <c>400 "O token deve ter pelo menos 32 caracteres."</c>) e não aceita sequência numérica nem
/// letra repetida quatro vezes — daí o token vir de <c>RandomNumberGenerator</c> em base64url,
/// que não produz nenhum dos dois padrões;</item>
/// <item><c>GET /v3/webhooks/{id}</c> devolve <c>hasAuthToken: true</c> e <strong>omite o
/// valor</strong> — o token só existe na resposta da criação, e perdê-lo obriga a recriar;</item>
/// <item>a resposta traz <c>interrupted</c> e <c>penalizedRequestsCount</c>, que são o sinal de
/// saúde da fila sequencial da conta;</item>
/// <item>evento inválido é recusado item a item (<c>"O evento [X] é inválido."</c>), então a
/// lista abaixo é a medida como aceita — <c>PIX_TRANSACTION_*</c> NÃO existe.</item>
/// </list>
/// </remarks>
internal sealed class AsaasWebhookProvisioner(
    AsaasClientProvider clientProvider,
    ILogger<AsaasWebhookProvisioner> logger) : IPaymentWebhookProvisioner
{
    private const string WEBHOOKS_PATH = "webhooks";

    /// <summary>Nome do webhook no painel do provedor — é como o dono da conta nos reconhece lá.</summary>
    private const string WEBHOOK_NAME = "Rufino BillPayment";

    /// <summary>32 bytes = 43 caracteres em base64url, acima do mínimo exigido pelo provedor.</summary>
    private const int AUTH_TOKEN_BYTES = 32;

    /// <summary>
    /// Os eventos que este BC consome, <strong>todos verificados como válidos na sonda</strong>.
    /// </summary>
    /// <remarks>
    /// A família <c>TRANSFER_*</c> é o que cobre o trilho Pix: o provedor não emite evento de
    /// transação Pix, e a saída aparece como transferência. Sem ela, cancelar um Pix no painel
    /// não chegaria aqui — foi exatamente o sintoma relatado em 2026-09-08.
    /// <c>TRANSFER_IN_BANK_ACCOUNT</c> está fora porque o provedor a recusa como inválida.
    /// </remarks>
    private static readonly string[] SUBSCRIBED_EVENTS =
    [
        "BILL_CREATED", "BILL_PENDING", "BILL_BANK_PROCESSING", "BILL_PAID",
        "BILL_CANCELLED", "BILL_FAILED", "BILL_REFUNDED",
        "TRANSFER_PENDING", "TRANSFER_BLOCKED", "TRANSFER_CANCELLED",
        "TRANSFER_DONE", "TRANSFER_FAILED",
    ];

    public async Task<WebhookProvisioningResult> EnsureAsync(
        CredentialRef? credential,
        string callbackUrl,
        string notificationEmail,
        CancellationToken cancellationToken)
    {
        var (http, reasonCode, _) = await clientProvider
            .CreateForAsync(credential, AsaasHttp.PAYMENT_CLIENT_NAME, cancellationToken);
        if (http is null)
            return WebhookProvisioningResult.Unavailable(reasonCode!);

        using var _ = http;

        // Idempotência: existindo webhook para a MESMA URL, ele é atualizado com token novo em
        // vez de duplicado. Sem isto, cada revínculo de chave deixaria mais um webhook vivo na
        // conta do tenant, e o provedor entregaria o mesmo evento N vezes.
        var (existing, listFailure) = await http.GetAsync<AsaasWebhookListResponse>(
            WEBHOOKS_PATH, logger, cancellationToken);

        if (listFailure is not null)
        {
            return listFailure.IsRetryable
                ? WebhookProvisioningResult.Unavailable(listFailure.ReasonCode)
                : WebhookProvisioningResult.Refused(listFailure.ReasonCode);
        }

        var authToken = GenerateAuthToken();
        var payload = new
        {
            name = WEBHOOK_NAME,
            url = callbackUrl,
            email = notificationEmail,
            enabled = true,
            interrupted = false,
            authToken,
            sendType = "SEQUENTIALLY",
            events = SUBSCRIBED_EVENTS,
        };

        var current = existing!.Data?.Find(w =>
            string.Equals(w.Url, callbackUrl, StringComparison.OrdinalIgnoreCase));

        var (body, failure) = current?.Id is { } id
            ? await http.PutAsync<AsaasWebhookResponse>($"{WEBHOOKS_PATH}/{id}", payload, logger, cancellationToken)
            : await http.PostAsync<AsaasWebhookResponse>(WEBHOOKS_PATH, payload, logger, cancellationToken);

        if (failure is not null)
        {
            return failure.IsRetryable
                ? WebhookProvisioningResult.Unavailable(failure.ReasonCode)
                : WebhookProvisioningResult.Refused(failure.ReasonCode);
        }

        return string.IsNullOrWhiteSpace(body!.Id)
            ? WebhookProvisioningResult.Refused("missing_webhook_id")
            : WebhookProvisioningResult.Provisioned(body.Id, authToken);
    }

    public async Task<WebhookHealthResult> GetHealthAsync(
        CredentialRef? credential,
        string providerWebhookId,
        CancellationToken cancellationToken)
    {
        var (http, reasonCode, _) = await clientProvider
            .CreateForAsync(credential, AsaasHttp.PAYMENT_CLIENT_NAME, cancellationToken);
        if (http is null)
            return WebhookHealthResult.Unavailable(reasonCode!);

        using var _ = http;
        var (body, failure) = await http.GetAsync<AsaasWebhookResponse>(
            $"{WEBHOOKS_PATH}/{Uri.EscapeDataString(providerWebhookId)}", logger, cancellationToken);

        if (failure is not null)
        {
            return failure.IsNotFound
                ? WebhookHealthResult.NotFound()
                : WebhookHealthResult.Unavailable(failure.ReasonCode);
        }

        return WebhookHealthResult.Healthy(
            body!.Enabled ?? false, body.Interrupted ?? false, body.PenalizedRequestsCount ?? 0);
    }

    public async Task<bool> RemoveAsync(
        CredentialRef? credential,
        string providerWebhookId,
        CancellationToken cancellationToken)
    {
        var (http, _, _) = await clientProvider
            .CreateForAsync(credential, AsaasHttp.PAYMENT_CLIENT_NAME, cancellationToken);
        if (http is null)
            return false;

        using var _ = http;
        var (body, failure) = await http.DeleteAsync<AsaasWebhookDeleteResponse>(
            $"{WEBHOOKS_PATH}/{Uri.EscapeDataString(providerWebhookId)}", logger, cancellationToken);

        // Já não existe É o desfecho desejado — remover o que não está lá não é falha.
        if (failure is not null)
            return failure.IsNotFound;

        return body?.Deleted ?? false;
    }

    /// <summary>
    /// Token de alta entropia. Base64url não produz sequência numérica longa nem a mesma letra
    /// quatro vezes seguidas — os dois padrões que o provedor recusa como token fraco.
    /// </summary>
    private static string GenerateAuthToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(AUTH_TOKEN_BYTES))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}

internal sealed class AsaasWebhookResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    /// <summary>
    /// A fila SEQUENCIAL desta conta está pausada. Interrompida, nenhum evento chega — nem os
    /// das outras ordens.
    /// </summary>
    [JsonPropertyName("interrupted")]
    public bool? Interrupted { get; set; }

    [JsonPropertyName("hasAuthToken")]
    public bool? HasAuthToken { get; set; }

    [JsonPropertyName("penalizedRequestsCount")]
    public int? PenalizedRequestsCount { get; set; }

    /// <summary>Só vem na CRIAÇÃO. O <c>GET</c> omite — medido em 2026-09-08.</summary>
    [JsonPropertyName("authToken")]
    public string? AuthToken { get; set; }
}

internal sealed class AsaasWebhookListResponse
{
    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; set; }

    [JsonPropertyName("data")]
    public List<AsaasWebhookResponse>? Data { get; set; }
}

internal sealed class AsaasWebhookDeleteResponse
{
    [JsonPropertyName("deleted")]
    public bool? Deleted { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
