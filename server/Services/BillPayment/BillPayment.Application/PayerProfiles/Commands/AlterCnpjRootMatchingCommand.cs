namespace BillPayment.Application.PayerProfiles.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record AlterCnpjRootMatchingCommand(
    Guid TenantId,
    bool Enabled) : ITenantScopedCommand, IRequest<AlterCnpjRootMatchingResponse>;

public sealed record AlterCnpjRootMatchingResponse(Guid Id);

public sealed class AlterCnpjRootMatchingCommandHandler(
    IPayerProfileRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AlterCnpjRootMatchingCommand, AlterCnpjRootMatchingResponse>
{
    public async Task<AlterCnpjRootMatchingResponse> Handle(
        AlterCnpjRootMatchingCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var profile = await repository.GetByTenantAsync(tenantId, cancellationToken)
            ?? throw PayerProfileErrors.NotFound(request.TenantId);

        profile.SetCnpjRootMatching(request.Enabled, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new AlterCnpjRootMatchingResponse(profile.Id.Value);
    }
}

public sealed class AlterCnpjRootMatchingIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<AlterCnpjRootMatchingIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<AlterCnpjRootMatchingCommand, AlterCnpjRootMatchingResponse>(mediator, requestManager, logger)
{
    protected override AlterCnpjRootMatchingResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
