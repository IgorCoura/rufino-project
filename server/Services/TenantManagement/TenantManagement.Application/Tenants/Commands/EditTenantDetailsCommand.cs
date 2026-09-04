namespace TenantManagement.Application.Tenants.Commands;

using TenantManagement.Application.Mediator;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.Tenants;
using Microsoft.Extensions.Logging;

public sealed record EditTenantDetailsCommand(
    Guid TenantId,
    string LegalName,
    string? TradeName) : IRequest<EditTenantDetailsResponse>;

public sealed record EditTenantDetailsResponse(Guid Id);

public sealed class EditTenantDetailsCommandHandler(
    ITenantRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<EditTenantDetailsCommand, EditTenantDetailsResponse>
{
    public async Task<EditTenantDetailsResponse> Handle(
        EditTenantDetailsCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await repository.GetByIdAsync(TenantId.From(request.TenantId), cancellationToken)
            ?? throw TenantErrors.NotFound(request.TenantId);

        tenant.Rename(request.LegalName, request.TradeName, timeProvider.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new EditTenantDetailsResponse(tenant.Id.Value);
    }
}

public sealed class EditTenantDetailsIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<EditTenantDetailsIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<EditTenantDetailsCommand, EditTenantDetailsResponse>(mediator, requestManager, logger)
{
    protected override EditTenantDetailsResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
