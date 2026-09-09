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
    /// Garante que existe um webhook apontando para <paramref name="callbackUrl"/>, habilitado e
    /// com a fila corrente. Idempotente: existindo um para a mesma URL, atualiza em vez de duplicar.
    /// </summary>
    /// <param name="authToken">
    /// O token a gravar no provedor. <c>null</c> gera um novo — <strong>é a rotação</strong>.
    /// Passar o token vigente (resolvido do cofre) é o caminho de CURA: reativa a fila, reabilita
    /// e corrige a URL sem trocar o segredo. A distinção importa porque o provedor não devolve o
    /// token em leitura nenhuma: cada rotação é uma janela em que uma falha entre gravar lá e
    /// gravar no cofre deixa o webhook mudo sem conserto que não seja recriá-lo.
    /// </param>
    Task<WebhookProvisioningResult> EnsureAsync(
        CredentialRef? credential,
        string callbackUrl,
        string notificationEmail,
        string? authToken,
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
/// <param name="Url">
/// Para onde o provedor está entregando HOJE. Comparada com a URL que esta instalação espera, é
/// o que denuncia webhook apontando para um endereço antigo — mudança de domínio não avisa.
/// </param>
public sealed record WebhookHealthResult(
    bool Found,
    bool Enabled,
    bool Interrupted,
    int PenalizedRequestsCount,
    string? Url,
    string? ReasonCode)
{
    /// <summary>Encontrado e entregando: habilitado, fila corrente e apontando para onde deve.</summary>
    public bool IsDelivering(string expectedUrl)
        => Found
            && Enabled
            && !Interrupted
            && string.Equals(Url, expectedUrl, StringComparison.OrdinalIgnoreCase);

    /// <summary>Não deu para saber — provedor fora do ar, credencial irresolvível.</summary>
    public bool IsUnavailable => !Found && ReasonCode is not null;

    public static WebhookHealthResult Healthy(
        bool enabled, bool interrupted, int penalizedRequestsCount, string? url)
        => new(Found: true, enabled, interrupted, penalizedRequestsCount, url, null);

    public static WebhookHealthResult NotFound()
        => new(Found: false, false, false, 0, null, null);

    public static WebhookHealthResult Unavailable(string reasonCode)
        => new(Found: false, false, false, 0, null, reasonCode);
}
