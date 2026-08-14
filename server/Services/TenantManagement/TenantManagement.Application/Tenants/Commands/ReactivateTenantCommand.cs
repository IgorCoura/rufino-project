namespace TenantManagement.Application.Tenants.Commands;

using TenantManagement.Application.Mediator;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;
using Microsoft.Extensions.Logging;

public sealed record ReactivateTenantCommand(Guid TenantId) : IRequest<ReactivateTenantResponse>;

public sealed record ReactivateTenantResponse(Guid Id, string Status);

public sealed class ReactivateTenantCommandHandler(
    ITenantRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<ReactivateTenantCommand, ReactivateTenantResponse>
{
    public async Task<ReactivateTenantResponse> Handle(
        ReactivateTenantCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await repository.GetByIdAsync(TenantId.From(request.TenantId), cancellationToken)
            ?? throw TenantErrors.NotFound(request.TenantId);

        tenant.Reactivate(timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ReactivateTenantResponse(tenant.Id.Value, tenant.Status.Name);
    }
}

public sealed class ReactivateTenantIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ReactivateTenantIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ReactivateTenantCommand, ReactivateTenantResponse>(mediator, requestManager, logger)
{
    protected override ReactivateTenantResponse CreateResultForDuplicateRequest() => new(Guid.Empty, string.Empty);
}
