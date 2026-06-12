namespace EconomicCore.Application.Commands.RegisterBundledPaymentEvent;

using EconomicCore.Application.Mediator;
using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

internal sealed class RegisterBundledPaymentEventHandler : IRequestHandler<RegisterBundledPaymentEventCommand, RegisterBundledPaymentEventResponse>
{
    private readonly IEconomicContractRepository _contractRepo;
    private readonly IEconomicEventRepository _eventRepo;
    private readonly TimeProvider _timeProvider;

    public RegisterBundledPaymentEventHandler(
        IEconomicContractRepository contractRepo,
        IEconomicEventRepository eventRepo,
        TimeProvider timeProvider)
    {
        _contractRepo = contractRepo;
        _eventRepo = eventRepo;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterBundledPaymentEventResponse> Handle(RegisterBundledPaymentEventCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var contractId = EconomicContractId.From(request.ContractId);
        var currency = Enumeration.FromDisplayName<Currency>(request.Currency);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var contract = await _contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken)
            ?? throw EconomicContractErrors.ContractNotFound(request.ContractId);

        var lines = new List<BundledPaymentLine>(request.Allocations.Count);
        var coveredCommitmentIds = new List<CommitmentId>(request.Allocations.Count);
        foreach (var allocation in request.Allocations)
        {
            var commitmentId = CommitmentId.From(allocation.CommitmentId);

            // Cada perna respeita o gate de cobertura do agregado (Active, OutflowPromise, status, valor exato).
            contract.EnsurePayable(commitmentId, allocation.Amount);

            // Anti-duplicata por perna: não cobre um commitment já coberto por outro pagamento.
            var existingCoverage = await _eventRepo.FindCoveredByCommitmentAsync(commitmentId, tenantId, cancellationToken);
            if (existingCoverage is not null)
                throw EconomicContractErrors.CommitmentAlreadyCovered(allocation.CommitmentId);

            lines.Add(new BundledPaymentLine(contract.Id.Value, allocation.CommitmentId, allocation.Amount));
            coveredCommitmentIds.Add(commitmentId);
        }

        // A política de competência do caixa (período mais antigo entre as pernas) vive no agregado.
        var competence = contract.ResolveBundledCompetence(coveredCommitmentIds);
        var createdBy = request.UserId is { } uid ? UserId.From(uid) : (UserId?)null;

        var paymentEvent = EconomicEvent.RegisterBundledPayment(
            EconomicEventId.New(),
            tenantId,
            EconomicResourceId.New(),
            currency,
            request.OccurredAt,
            payerAgentId: EconomicAgentId.From(tenantId.Value),
            payeeAgentId: contract.CounterpartyId,
            lines,
            competenceYear: competence.Year,
            competenceMonth: competence.Month,
            createdBy: createdBy,
            registeredAt: now);

        await _eventRepo.InsertAsync(paymentEvent, cancellationToken);
        await _eventRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterBundledPaymentEventResponse(
            paymentEvent.Id.Value,
            paymentEvent.Direction.Name,
            paymentEvent.Amount.Amount,
            paymentEvent.Allocations.Count);
    }
}

internal sealed class RegisterBundledPaymentEventIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RegisterBundledPaymentEventIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RegisterBundledPaymentEventCommand, RegisterBundledPaymentEventResponse>(mediator, requestManager, logger)
{
    protected override RegisterBundledPaymentEventResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, 0m, 0);
}
