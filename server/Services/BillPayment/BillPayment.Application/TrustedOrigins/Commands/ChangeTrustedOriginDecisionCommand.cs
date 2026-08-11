namespace BillPayment.Application.TrustedOrigins.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using Microsoft.Extensions.Logging;

public sealed record ChangeTrustedOriginDecisionCommand(
    Guid TenantId,
    Guid TrustedOriginId,
    string Decision,
    Guid DecidedBy,
    string? Note) : IRequest<ChangeTrustedOriginDecisionResponse>;

public sealed record ChangeTrustedOriginDecisionResponse(Guid Id);

public sealed class ChangeTrustedOriginDecisionCommandHandler(
    ITrustedOriginRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ChangeTrustedOriginDecisionCommand, ChangeTrustedOriginDecisionResponse>
{
    public async Task<ChangeTrustedOriginDecisionResponse> Handle(
        ChangeTrustedOriginDecisionCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var originId = TrustedOriginId.From(request.TrustedOriginId);

        var origin = await repository.GetAsync(tenantId, originId, cancellationToken)
            ?? throw TrustedOriginErrors.NotFound(request.TrustedOriginId);

        var decision = Enumeration.FromDisplayName<TrustDecision>(request.Decision);

        origin.ChangeDecision(decision, UserId.From(request.DecidedBy), request.Note, DateTime.UtcNow);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ChangeTrustedOriginDecisionResponse(origin.Id.Value);
    }
}

public sealed class ChangeTrustedOriginDecisionIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ChangeTrustedOriginDecisionIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ChangeTrustedOriginDecisionCommand, ChangeTrustedOriginDecisionResponse>(mediator, requestManager, logger)
{
    protected override ChangeTrustedOriginDecisionResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
