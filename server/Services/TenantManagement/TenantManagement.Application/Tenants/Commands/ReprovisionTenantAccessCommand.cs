namespace TenantManagement.Application.Tenants.Commands;

using TenantManagement.Application.Mediator;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;
using Microsoft.Extensions.Logging;

/// <summary>
/// Conserta um provisionamento que não chegou ao provedor de identidade. É o caminho de
/// volta do único ponto onde este BC não é transacional — e é idempotente de propósito:
/// reexecutar sem risco é o que faz alguém de fato usar o botão.
/// </summary>
public sealed record ReprovisionTenantAccessCommand(Guid TenantId) : IRequest<ReprovisionTenantAccessResponse>;

public sealed record ReprovisionTenantAccessResponse(Guid Id, int RequeuedMemberships, string Provisioning);

public sealed class ReprovisionTenantAccessCommandHandler(
    ITenantRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<ReprovisionTenantAccessCommand, ReprovisionTenantAccessResponse>
{
    public async Task<ReprovisionTenantAccessResponse> Handle(
        ReprovisionTenantAccessCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await repository.GetByIdAsync(TenantId.From(request.TenantId), cancellationToken)
            ?? throw TenantErrors.NotFound(request.TenantId);

        var requeued = tenant.RequeueFailedAccessProvisioning(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ReprovisionTenantAccessResponse(tenant.Id.Value, requeued.Count, tenant.AccessProvisioning.Name);
    }
}

public sealed class ReprovisionTenantAccessIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ReprovisionTenantAccessIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ReprovisionTenantAccessCommand, ReprovisionTenantAccessResponse>(mediator, requestManager, logger)
{
    protected override ReprovisionTenantAccessResponse CreateResultForDuplicateRequest() => new(Guid.Empty, 0, string.Empty);
}
