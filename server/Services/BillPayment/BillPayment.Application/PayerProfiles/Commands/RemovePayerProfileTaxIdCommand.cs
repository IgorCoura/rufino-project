namespace BillPayment.Application.PayerProfiles.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record RemovePayerProfileTaxIdCommand(
    Guid TenantId,
    string TaxId) : IRequest<RemovePayerProfileTaxIdResponse>;

public sealed record RemovePayerProfileTaxIdResponse(Guid Id);

public sealed class RemovePayerProfileTaxIdCommandHandler(
    IPayerProfileRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemovePayerProfileTaxIdCommand, RemovePayerProfileTaxIdResponse>
{
    public async Task<RemovePayerProfileTaxIdResponse> Handle(
        RemovePayerProfileTaxIdCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var profile = await repository.GetByTenantAsync(tenantId, cancellationToken)
            ?? throw PayerProfileErrors.NotFound(request.TenantId);

        profile.RemoveAdditionalTaxId(request.TaxId, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RemovePayerProfileTaxIdResponse(profile.Id.Value);
    }
}

public sealed class RemovePayerProfileTaxIdIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RemovePayerProfileTaxIdIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RemovePayerProfileTaxIdCommand, RemovePayerProfileTaxIdResponse>(mediator, requestManager, logger)
{
    protected override RemovePayerProfileTaxIdResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
