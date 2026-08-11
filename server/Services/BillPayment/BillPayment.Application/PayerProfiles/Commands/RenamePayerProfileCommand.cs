namespace BillPayment.Application.PayerProfiles.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record RenamePayerProfileCommand(
    Guid TenantId,
    string LegalName) : IRequest<RenamePayerProfileResponse>;

public sealed record RenamePayerProfileResponse(Guid Id);

public sealed class RenamePayerProfileCommandHandler(
    IPayerProfileRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RenamePayerProfileCommand, RenamePayerProfileResponse>
{
    public async Task<RenamePayerProfileResponse> Handle(
        RenamePayerProfileCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var profile = await repository.GetByTenantAsync(tenantId, cancellationToken)
            ?? throw PayerProfileErrors.NotFound(request.TenantId);

        profile.Rename(request.LegalName, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RenamePayerProfileResponse(profile.Id.Value);
    }
}

public sealed class RenamePayerProfileIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RenamePayerProfileIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RenamePayerProfileCommand, RenamePayerProfileResponse>(mediator, requestManager, logger)
{
    protected override RenamePayerProfileResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
