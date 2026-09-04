namespace TenantManagement.Application.Tenants.Commands;

using TenantManagement.Application.Mediator;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;
using Microsoft.Extensions.Logging;

/// <summary>
/// Conceder e revogar acesso. A resposta devolve o estado do provisionamento porque o
/// provedor de identidade não participa desta transação: dizer "concedido" sem qualificar
/// esconderia o caso em que o acesso ainda não chegou lá.
/// </summary>
public sealed record GrantTenantMembershipCommand(
    Guid TenantId,
    string Email,
    string Role) : IRequest<TenantMembershipResponse>;

public sealed record RevokeTenantMembershipCommand(
    Guid TenantId,
    string Email) : IRequest<TenantMembershipResponse>;

public sealed record TenantMembershipResponse(Guid Id, string Email, string Provisioning);

public sealed class GrantTenantMembershipCommandHandler(
    ITenantRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<GrantTenantMembershipCommand, TenantMembershipResponse>
{
    public async Task<TenantMembershipResponse> Handle(
        GrantTenantMembershipCommand request,
        CancellationToken cancellationToken)
    {
        var role = TenantInput.ParseRole(request.Role);

        var tenant = await repository.GetByIdAsync(TenantId.From(request.TenantId), cancellationToken)
            ?? throw TenantErrors.NotFound(request.TenantId);

        tenant.GrantMembership(request.Email, role, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new TenantMembershipResponse(tenant.Id.Value, request.Email, tenant.AccessProvisioning.Name);
    }
}

public sealed class RevokeTenantMembershipCommandHandler(
    ITenantRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<RevokeTenantMembershipCommand, TenantMembershipResponse>
{
    public async Task<TenantMembershipResponse> Handle(
        RevokeTenantMembershipCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await repository.GetByIdAsync(TenantId.From(request.TenantId), cancellationToken)
            ?? throw TenantErrors.NotFound(request.TenantId);

        tenant.RevokeMembership(request.Email, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new TenantMembershipResponse(tenant.Id.Value, request.Email, tenant.AccessProvisioning.Name);
    }
}

public sealed class GrantTenantMembershipIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<GrantTenantMembershipIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<GrantTenantMembershipCommand, TenantMembershipResponse>(mediator, requestManager, logger)
{
    protected override TenantMembershipResponse CreateResultForDuplicateRequest() => new(Guid.Empty, string.Empty, string.Empty);
}

public sealed class RevokeTenantMembershipIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RevokeTenantMembershipIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RevokeTenantMembershipCommand, TenantMembershipResponse>(mediator, requestManager, logger)
{
    protected override TenantMembershipResponse CreateResultForDuplicateRequest() => new(Guid.Empty, string.Empty, string.Empty);
}
