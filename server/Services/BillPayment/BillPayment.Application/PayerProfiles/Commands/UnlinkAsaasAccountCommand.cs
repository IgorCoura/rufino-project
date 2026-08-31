namespace BillPayment.Application.PayerProfiles.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Desvincula a subconta Asaas e remove o segredo do cofre na mesma unidade de trabalho.
/// Idempotente: sem vínculo, não há nada a fazer e a resposta é a mesma.
/// </summary>
public sealed record UnlinkAsaasAccountCommand(
    Guid TenantId) : ITenantScopedCommand, IRequest<UnlinkAsaasAccountResponse>;

public sealed record UnlinkAsaasAccountResponse(Guid Id, bool CanSchedulePayments);

public sealed class UnlinkAsaasAccountCommandHandler(
    IPayerProfileRepository repository,
    ISecretVault vault,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
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
        if (previous is not null)
        {
            profile.UnlinkAsaasAccount(clock.GetUtcNow().UtcDateTime);
            await vault.RemoveAsync(previous, cancellationToken);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);
        }

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
