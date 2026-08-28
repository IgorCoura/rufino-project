namespace BillPayment.Application.PayerProfiles.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record AddPayerProfileTaxIdCommand(
    Guid TenantId,
    string TaxId) : IRequest<AddPayerProfileTaxIdResponse>, ISensitiveCommand;

public sealed record AddPayerProfileTaxIdResponse(Guid Id);

public sealed class AddPayerProfileTaxIdCommandHandler(
    IPayerProfileRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddPayerProfileTaxIdCommand, AddPayerProfileTaxIdResponse>
{
    public async Task<AddPayerProfileTaxIdResponse> Handle(
        AddPayerProfileTaxIdCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var profile = await repository.GetByTenantAsync(tenantId, cancellationToken)
            ?? throw PayerProfileErrors.NotFound(request.TenantId);

        profile.AddAdditionalTaxId(request.TaxId, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new AddPayerProfileTaxIdResponse(profile.Id.Value);
    }
}

public sealed class AddPayerProfileTaxIdIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<AddPayerProfileTaxIdIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<AddPayerProfileTaxIdCommand, AddPayerProfileTaxIdResponse>(mediator, requestManager, logger)
{
    protected override AddPayerProfileTaxIdResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
