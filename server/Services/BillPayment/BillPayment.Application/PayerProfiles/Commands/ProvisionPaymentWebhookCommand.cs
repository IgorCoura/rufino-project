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
/// Cria (ou conserta) o webhook da conta Asaas do tenant e guarda o token no cofre.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Roda FORA da transação que vincula a chave</strong>, disparado por evento — mesma
/// doutrina do <c>CreatePaymentOrderForBillCommand</c>. Chamar o provedor dentro daquela
/// transação faria o vínculo da chave falhar por indisponibilidade de um passo que não é dele,
/// e o tenant ficaria sem conta configurada por causa do webhook.
/// </para>
/// <para>
/// <strong>Idempotente</strong>: o adapter adota o webhook existente para a mesma URL (ou para o
/// mesmo nome, quando a URL mudou) em vez de duplicar.
/// </para>
/// <para>
/// <strong>Este comando é o ATUADOR, não o decisor.</strong> Quem observa o provedor e escolhe
/// entre rotacionar, curar e não fazer nada é o <c>SweepPaymentWebhookCommand</c>. Aqui só se
/// executa o que já foi decidido — foi assim que o vínculo de chave e a varredura passaram a
/// caber no mesmo caminho sem duplicar a gravação do cofre.
/// </para>
/// </remarks>
/// <param name="RotateToken">
/// <c>true</c> gera um token novo; <c>false</c> reaproveita o do cofre quando existe. Rotacionar
/// a cada conserto trocaria o segredo a cada meia hora, e cada troca é uma janela em que falhar
/// entre o provedor e o cofre deixa o webhook mudo sem conserto que não seja recriá-lo — o
/// provedor não devolve o token em leitura nenhuma.
/// </param>
public sealed record ProvisionPaymentWebhookCommand(Guid TenantId, bool RotateToken = true)
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
    public const string OUTCOME_PROVISIONED = "Provisioned";
    public const string OUTCOME_SKIPPED = "Skipped";
    public const string OUTCOME_FAILED = "Failed";

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

        // Reaproveitar exige conseguir LER o token. Cofre indisponível não vira webhook sem
        // token: cai para rotação, que é o caminho que reconstrói o segredo dos dois lados.
        var reusedToken = request.RotateToken ? null : await TryResolveTokenAsync(profile, cancellationToken);

        var result = await provisioner.EnsureAsync(
            profile.AsaasAccountRef, callbackUrl, settings.NotificationEmail, reusedToken, cancellationToken);

        if (!result.IsProvisioned)
        {
            // Retentável sobe para o outbox tentar de novo com backoff; recusa permanente fica
            // registrada e visível no perfil, sem derrubar nada. Nos dois casos a varredura de
            // saúde volta a este tenant no próximo ciclo — o outbox tem backoff FINITO, e a
            // varredura é o que impede que esgotá-lo signifique nunca mais.
            if (result.IsRetryable)
                throw PayerProfileErrors.AsaasProviderUnreachable(result.ReasonCode!);

            logger.LogError(
                "O provedor recusou provisionar o webhook do tenant {TenantId} ({Reason}).",
                request.TenantId, result.ReasonCode);

            return new ProvisionPaymentWebhookResponse(OUTCOME_FAILED, result.ReasonCode);
        }

        var now = clock.GetUtcNow().UtcDateTime;

        if (reusedToken is not null)
        {
            // Cura: o segredo no cofre continua valendo, e o carimbo de rotação NÃO se mexe —
            // ele é o relógio da higiene do token, não o do último conserto.
            profile.ConfirmAsaasWebhook(result.ProviderWebhookId!, now);
        }
        else
        {
            // O token substitui o anterior mantendo a referência quando já existia — é o caminho
            // do refresh do cofre, e evita credencial órfã a cada reprovisionamento.
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

            profile.LinkAsaasWebhook(webhookRef, result.ProviderWebhookId!, now);
        }

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Webhook do tenant {TenantId} {Action} no provedor ({WebhookId}).",
                request.TenantId,
                reusedToken is null ? "provisionado com token novo" : "reconfirmado sem trocar o token",
                result.ProviderWebhookId);
        }

        return new ProvisionPaymentWebhookResponse(OUTCOME_PROVISIONED, null);
    }

    /// <summary>
    /// O token vigente do cofre, ou <c>null</c> quando não há como recuperá-lo.
    /// </summary>
    /// <remarks>
    /// Falhar aqui NÃO é erro: é a resposta "não dá para reaproveitar", e quem chama trata isso
    /// rotacionando. O valor nunca é logado.
    /// </remarks>
    private async Task<string?> TryResolveTokenAsync(
        PayerProfile profile, CancellationToken cancellationToken)
    {
        if (profile.AsaasWebhookRef is not { } webhookRef)
            return null;

        try
        {
            var token = await vault.ResolveAsync(webhookRef, cancellationToken);
            return string.IsNullOrWhiteSpace(token) ? null : token;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(
                ex,
                "Não foi possível ler o token de webhook do tenant no cofre; será gerado um novo.");

            return null;
        }
    }
}

/// <summary>
/// O par de idempotência do reprovisionamento manual.
/// </summary>
/// <remarks>
/// Existe porque o endpoint administrativo embrulha o comando em <c>IdentifiedCommand</c>, e o
/// mediator resolve o handler pelo tipo CONCRETO — sem esta subclasse a rota falharia em runtime
/// por handler não encontrado. O caminho por evento (outbox) não passa por aqui: quem garante
/// unicidade lá é a própria mensagem.
/// </remarks>
public sealed class ProvisionPaymentWebhookIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ProvisionPaymentWebhookIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ProvisionPaymentWebhookCommand, ProvisionPaymentWebhookResponse>(
        mediator, requestManager, logger)
{
    protected override ProvisionPaymentWebhookResponse CreateResultForDuplicateRequest()
        => new(ProvisionPaymentWebhookCommandHandler.OUTCOME_SKIPPED, "duplicate_request");
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
/// <remarks>
/// Chave nova é conta possivelmente nova, e o token da conta anterior não vale para ela — por
/// isso o vínculo sempre ROTACIONA, nunca reaproveita.
/// </remarks>
public sealed class ProvisionWebhookOnAsaasAccountLinkedHandler(IMediator mediator)
    : IDomainEventHandler<AsaasAccountLinkedDomainEvent>
{
    public async Task HandleAsync(
        AsaasAccountLinkedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await mediator.Send(
            new ProvisionPaymentWebhookCommand(domainEvent.TenantId.Value, RotateToken: true),
            cancellationToken);
    }
}
