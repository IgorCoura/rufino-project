namespace BillPayment.Domain.Ports;

using BillPayment.Domain.Secrets;

/// <summary>
/// Cria, confere e remove o webhook da conta do tenant no provedor de pagamento.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Um webhook por conta</strong> (ADR-016): não há conta-plataforma, então cada tenant
/// precisa do seu, provisionado com a chave dele. Mesmas regras das portas irmãs: falha é
/// modelada, nunca lançada, e a credencial é sempre a do tenant.
/// </para>
/// <para>
/// <strong>O token de autenticação é criado no provisionamento e não é recuperável depois</strong>
/// — medido em sandbox (2026-09-08): <c>GET /v3/webhooks/{id}</c> devolve apenas
/// <c>hasAuthToken: true</c>, nunca o valor. Quem chama tem a única chance de guardá-lo.
/// </para>
/// </remarks>
public interface IPaymentWebhookProvisioner
{
    /// <summary>
    /// Garante que existe um webhook apontando para <paramref name="callbackUrl"/>, com token
    /// novo. Idempotente: existindo um para a mesma URL, atualiza em vez de duplicar.
    /// </summary>
    Task<WebhookProvisioningResult> EnsureAsync(
        CredentialRef? credential,
        string callbackUrl,
        string notificationEmail,
        CancellationToken cancellationToken);

    /// <summary>Lê o estado do webhook — é daqui que sai o sinal de fila interrompida.</summary>
    Task<WebhookHealthResult> GetHealthAsync(
        CredentialRef? credential,
        string providerWebhookId,
        CancellationToken cancellationToken);

    Task<bool> RemoveAsync(
        CredentialRef? credential,
        string providerWebhookId,
        CancellationToken cancellationToken);
}

/// <summary>
/// O que voltou do provisionamento: o webhook criado, ou o motivo de não haver.
/// </summary>
/// <remarks>
/// <see cref="AuthToken"/> é segredo ao portador — vai direto para o cofre e <strong>nunca</strong>
/// é logado nem devolvido por API.
/// </remarks>
public sealed record WebhookProvisioningResult(
    string? ProviderWebhookId,
    string? AuthToken,
    string? ReasonCode,
    bool IsRetryable)
{
    public bool IsProvisioned => ProviderWebhookId is not null && AuthToken is not null;

    public static WebhookProvisioningResult Provisioned(string providerWebhookId, string authToken)
        => new(providerWebhookId, authToken, null, IsRetryable: false);

    public static WebhookProvisioningResult Refused(string reasonCode)
        => new(null, null, reasonCode, IsRetryable: false);

    public static WebhookProvisioningResult Unavailable(string reasonCode)
        => new(null, null, reasonCode, IsRetryable: true);
}

/// <param name="Interrupted">
/// O provedor pausou a entrega desta conta. Como a fila é <strong>sequencial</strong>, uma fila
/// interrompida significa que NENHUM evento da conta chega — e o silêncio é indistinguível de
/// "nada aconteceu" sem este sinal.
/// </param>
/// <param name="PenalizedRequestsCount">
/// Quantas entregas o provedor já contou como falhas. Crescendo, a interrupção é o próximo passo.
/// </param>
public sealed record WebhookHealthResult(
    bool Found,
    bool Enabled,
    bool Interrupted,
    int PenalizedRequestsCount,
    string? ReasonCode)
{
    public static WebhookHealthResult Healthy(bool enabled, bool interrupted, int penalizedRequestsCount)
        => new(Found: true, enabled, interrupted, penalizedRequestsCount, null);

    public static WebhookHealthResult NotFound()
        => new(Found: false, false, false, 0, null);

    public static WebhookHealthResult Unavailable(string reasonCode)
        => new(Found: false, false, false, 0, reasonCode);
}
