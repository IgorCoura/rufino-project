namespace BillPayment.Application.PayerProfiles.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record RegisterPayerProfileCommand(
    Guid TenantId,
    string Kind,
    string LegalName,
    string PrimaryTaxId) : IRequest<RegisterPayerProfileResponse>, ISensitiveCommand;

public sealed record RegisterPayerProfileResponse(Guid Id);

public sealed class RegisterPayerProfileCommandHandler(
    IPayerProfileRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterPayerProfileCommand, RegisterPayerProfileResponse>
{
    public async Task<RegisterPayerProfileResponse> Handle(
        RegisterPayerProfileCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        // Tradução de input: converter string em Smart Enum é responsabilidade do handler.
        var kind = Enumeration.FromDisplayName<PayerKind>(request.Kind);

        if (await repository.ExistsForTenantAsync(tenantId, cancellationToken))
            throw PayerProfileErrors.TenantAlreadyHasProfile();

        var profile = PayerProfile.Register(
            tenantId,
            kind,
            request.LegalName,
            request.PrimaryTaxId,
            DateTime.UtcNow);

        await repository.AddAsync(profile, cancellationToken);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterPayerProfileResponse(profile.Id.Value);
    }
}

public sealed class RegisterPayerProfileIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RegisterPayerProfileIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RegisterPayerProfileCommand, RegisterPayerProfileResponse>(mediator, requestManager, logger)
{
    protected override RegisterPayerProfileResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
