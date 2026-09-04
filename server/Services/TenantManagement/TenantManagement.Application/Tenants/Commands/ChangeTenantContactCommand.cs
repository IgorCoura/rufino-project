namespace TenantManagement.Application.Tenants.Commands;

using TenantManagement.Application.Mediator;
using TenantManagement.Domain.SeedWork;
using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;
using Microsoft.Extensions.Logging;

public sealed record ChangeTenantContactCommand(
    Guid TenantId,
    string Email,
    string? Phone) : IRequest<ChangeTenantContactResponse>;

public sealed record ChangeTenantContactResponse(Guid Id);

public sealed class ChangeTenantContactCommandHandler(
    ITenantRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<ChangeTenantContactCommand, ChangeTenantContactResponse>
{
    public async Task<ChangeTenantContactResponse> Handle(
        ChangeTenantContactCommand request,
        CancellationToken cancellationToken)
    {
        var tenant = await repository.GetByIdAsync(TenantId.From(request.TenantId), cancellationToken)
            ?? throw TenantErrors.NotFound(request.TenantId);

        tenant.ChangeContact(
            ContactInfo.Create(request.Email, request.Phone),
            timeProvider.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ChangeTenantContactResponse(tenant.Id.Value);
    }
}

public sealed class ChangeTenantContactIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ChangeTenantContactIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ChangeTenantContactCommand, ChangeTenantContactResponse>(mediator, requestManager, logger)
{
    protected override ChangeTenantContactResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
