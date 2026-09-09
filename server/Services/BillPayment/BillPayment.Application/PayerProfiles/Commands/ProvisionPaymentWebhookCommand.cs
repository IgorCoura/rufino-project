namespace BillPayment.Application.PayerProfiles.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Cria (ou renova) o webhook da conta Asaas do tenant e guarda o token no cofre.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Roda FORA da transação que vincula a chave</strong>, disparado por evento — mesma
/// doutrina do <c>CreatePaymentOrderForBillCommand</c>. Chamar o provedor dentro daquela
/// transação faria o vínculo da chave falhar por indisponibilidade de um passo que não é dele,
/// e o tenant ficaria sem conta configurada por causa do webhook.
/// </para>
/// <para>
/// <strong>Idempotente</strong>: o adapter adota o webhook existente para a mesma URL em vez de
/// duplicar, e reentrega do outbox só troca o token — que é o comportamento desejado, porque o
/// token antigo deixa de valer no mesmo instante em que o novo é gravado.
/// </para>
/// </remarks>
public sealed record ProvisionPaymentWebhookCommand(Guid TenantId)
    : ITenantScopedCommand, IRequest<ProvisionPaymentWebhookResponse>;

public sealed record ProvisionPaymentWebhookResponse(string Outcome, string? ReasonCode);

public sealed class ProvisionPaymentWebhookCommandHandler(
    IPayerProfileRepository payerProfiles,
    IPaymentWebhookProvisioner provisioner,
    ISecretVault vault,
    IOptions<PaymentWebhookOptions> options,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<ProvisionPaymentWebhookCommandHandler> logger)
    : IRequestHandler<ProvisionPaymentWebhookCommand, ProvisionPaymentWebhookResponse>
{
    private const string OUTCOME_PROVISIONED = "Provisioned";
    private const string OUTCOME_SKIPPED = "Skipped";
    private const string OUTCOME_FAILED = "Failed";

    public async Task<ProvisionPaymentWebhookResponse> Handle(
        ProvisionPaymentWebhookCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken)
            ?? throw PayerProfileErrors.NotFound(request.TenantId);

        // A chave pode ter sido desvinculada entre o evento e este handler. Sem ela não há como
        // falar com o provedor, e provisionar não é o que destrava isso.
        if (!profile.CanSchedulePayments)
            return new ProvisionPaymentWebhookResponse(OUTCOME_SKIPPED, "tenant_key_not_configured");

        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.PublicBaseUrl))
        {
            // Sem URL pública não há webhook possível — e vale dizer alto: a instalação inteira
            // fica dependendo do polling, que é justamente o que se quer parar de depender.
            logger.LogWarning(
                "PaymentWebhook:PublicBaseUrl não configurada; o webhook do tenant não será provisionado.");

            return new ProvisionPaymentWebhookResponse(OUTCOME_SKIPPED, "public_base_url_missing");
        }

        var callbackUrl = settings.BuildCallbackUrl(request.TenantId);

        var result = await provisioner.EnsureAsync(
            profile.AsaasAccountRef, callbackUrl, settings.NotificationEmail, cancellationToken);

        if (!result.IsProvisioned)
        {
            // Retentável sobe para o outbox tentar de novo com backoff; recusa permanente fica
            // registrada e visível no perfil, sem derrubar nada.
            if (result.IsRetryable)
                throw PayerProfileErrors.AsaasProviderUnreachable(result.ReasonCode!);

            logger.LogError(
                "O provedor recusou provisionar o webhook do tenant {TenantId} ({Reason}).",
                request.TenantId, result.ReasonCode);

            return new ProvisionPaymentWebhookResponse(OUTCOME_FAILED, result.ReasonCode);
        }

        // O token substitui o anterior mantendo a referência quando já existia — é o caminho do
        // refresh do cofre, e evita credencial órfã a cada reprovisionamento.
        CredentialRef webhookRef;
        if (profile.AsaasWebhookRef is { } existing)
        {
            await vault.ReplaceAsync(existing, result.AuthToken!, cancellationToken);
            webhookRef = existing;
        }
        else
        {
            webhookRef = await vault.StoreAsync(
                tenantId, SecretKind.AsaasWebhookToken, result.AuthToken!, cancellationToken);
        }

        profile.LinkAsaasWebhook(webhookRef, result.ProviderWebhookId!, clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Webhook do tenant {TenantId} provisionado no provedor ({WebhookId}).",
                request.TenantId, result.ProviderWebhookId);
        }

        return new ProvisionPaymentWebhookResponse(OUTCOME_PROVISIONED, null);
    }
}

/// <summary>
/// Onde o provedor deve entregar os eventos, e para quem avisar quando a fila dele quebrar.
/// </summary>
/// <remarks>
/// <strong>Não guarda segredo nenhum</strong> — desde 2026-09-08 o token é por tenant e vive no
/// cofre. O que resta aqui é endereço, que é público por natureza.
/// </remarks>
public sealed class PaymentWebhookOptions
{
    public const string SectionName = "PaymentWebhook";

    /// <summary>A base pública desta API, como o provedor a alcança (com esquema, sem barra final).</summary>
    public string? PublicBaseUrl { get; set; }

    /// <summary>Para onde o provedor avisa quando interrompe a fila da conta.</summary>
    public string NotificationEmail { get; set; } = string.Empty;

    public string BuildCallbackUrl(Guid tenantId)
        => $"{PublicBaseUrl!.TrimEnd('/')}/webhooks/asaas/{tenantId}";
}

/// <summary>
/// Chave vinculada → webhook provisionado. Vive aqui pelo mesmo motivo dos demais handlers de
/// evento: precisa do mediator, e <c>Infra → Application</c> seria ciclo.
/// </summary>
public sealed class ProvisionWebhookOnAsaasAccountLinkedHandler(IMediator mediator)
    : IDomainEventHandler<AsaasAccountLinkedDomainEvent>
{
    public async Task HandleAsync(
        AsaasAccountLinkedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await mediator.Send(
            new ProvisionPaymentWebhookCommand(domainEvent.TenantId.Value), cancellationToken);
    }
}
