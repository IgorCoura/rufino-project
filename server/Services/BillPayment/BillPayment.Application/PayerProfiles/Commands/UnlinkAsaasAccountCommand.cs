namespace BillPayment.Application.PayerProfiles.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Desvincula a subconta Asaas, derruba o webhook dela no provedor e remove os DOIS segredos do
/// cofre. Idempotente: sem vínculo, não há nada a fazer e a resposta é a mesma.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O webhook sai junto, e isso não é zelo — é correção.</strong> Até 2026-09-09 o
/// desvínculo limpava só o ponteiro da chave: o webhook continuava vivo na conta do cliente
/// apontando para esta API, o token dele ficava órfão no cofre, e o perfil seguia respondendo
/// <c>HasWebhook</c> com <c>AsaasAccountRef</c> nulo — de modo que todo evento entrante passava
/// na conferência do token e depois morria na releitura, em laço, sem ninguém aplicar nada.
/// </para>
/// <para>
/// <strong>A ordem importa:</strong> o provedor é chamado ANTES de o segredo da chave sair do
/// cofre, porque é com essa chave que se fala com ele.
/// </para>
/// </remarks>
public sealed record UnlinkAsaasAccountCommand(
    Guid TenantId) : ITenantScopedCommand, IRequest<UnlinkAsaasAccountResponse>;

public sealed record UnlinkAsaasAccountResponse(Guid Id, bool CanSchedulePayments);

public sealed class UnlinkAsaasAccountCommandHandler(
    IPayerProfileRepository repository,
    IPaymentWebhookProvisioner provisioner,
    ISecretVault vault,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<UnlinkAsaasAccountCommandHandler> logger)
    : IRequestHandler<UnlinkAsaasAccountCommand, UnlinkAsaasAccountResponse>
{
    public async Task<UnlinkAsaasAccountResponse> Handle(
        UnlinkAsaasAccountCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var profile = await repository.GetByTenantAsync(tenantId, cancellationToken)
            ?? throw PayerProfileErrors.NotFound(request.TenantId);

        var previous = profile.AsaasAccountRef;
        if (previous is null)
            return new UnlinkAsaasAccountResponse(profile.Id.Value, profile.CanSchedulePayments);

        var now = clock.GetUtcNow().UtcDateTime;
        var webhookRef = profile.AsaasWebhookRef;

        if (profile.AsaasWebhookId is { } webhookId)
        {
            // Best-effort DE PROPÓSITO: provedor fora do ar não pode impedir o tenant de tirar a
            // chave dele daqui. O que fica para trás é um webhook que passará a receber 401 e a
            // ser interrompido pelo próprio provedor — visível no painel dele, sem efeito aqui.
            var removed = await provisioner.RemoveAsync(previous, webhookId, cancellationToken);
            if (!removed)
            {
                logger.LogWarning(
                    "Não foi possível remover o webhook {WebhookId} do tenant {TenantId} no provedor. "
                    + "Ele continua cadastrado na conta e precisa de remoção manual no painel.",
                    webhookId, request.TenantId);
            }
        }

        profile.UnlinkAsaasWebhook(now);
        profile.UnlinkAsaasAccount(now);

        await vault.RemoveAsync(previous, cancellationToken);

        if (webhookRef is not null)
            await vault.RemoveAsync(webhookRef, cancellationToken);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new UnlinkAsaasAccountResponse(profile.Id.Value, profile.CanSchedulePayments);
    }
}

public sealed class UnlinkAsaasAccountIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<UnlinkAsaasAccountIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<UnlinkAsaasAccountCommand, UnlinkAsaasAccountResponse>(mediator, requestManager, logger)
{
    protected override UnlinkAsaasAccountResponse CreateResultForDuplicateRequest() => new(Guid.Empty, false);
}
