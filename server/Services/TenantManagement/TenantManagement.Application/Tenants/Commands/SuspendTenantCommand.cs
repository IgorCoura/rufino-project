namespace TenantManagement.Application.Tenants.Commands;

using TenantManagement.Application.Mediator;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;
using Microsoft.Extensions.Logging;

public sealed record SuspendTenantCommand(
    Guid TenantId,
    string Reason) : IRequest<SuspendTenantResponse>;

public sealed record SuspendTenantResponse(Guid Id, string Status);

public sealed class SuspendTenantCommandHandler(
    ITenantRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<SuspendTenantCommand, SuspendTenantResponse>
{
    public async Task<SuspendTenantResponse> Handle(
        SuspendTenantCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await repository.GetByIdAsync(TenantId.From(request.TenantId), cancellationToken)
            ?? throw TenantErrors.NotFound(request.TenantId);

        tenant.Suspend(request.Reason, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new SuspendTenantResponse(tenant.Id.Value, tenant.Status.Name);
    }
}

public sealed class SuspendTenantIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<SuspendTenantIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<SuspendTenantCommand, SuspendTenantResponse>(mediator, requestManager, logger)
{
    protected override SuspendTenantResponse CreateResultForDuplicateRequest() => new(Guid.Empty, string.Empty);
}
