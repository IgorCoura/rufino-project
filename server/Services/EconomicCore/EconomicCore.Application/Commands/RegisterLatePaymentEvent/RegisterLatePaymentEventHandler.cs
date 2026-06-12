namespace EconomicCore.Application.Commands.RegisterLatePaymentEvent;

using EconomicCore.Application.Mediator;
using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

internal sealed class RegisterLatePaymentEventHandler : IRequestHandler<RegisterLatePaymentEventCommand, RegisterLatePaymentEventResponse>
{
    private readonly IEconomicContractRepository _contractRepo;
    private readonly IEconomicEventRepository _eventRepo;
    private readonly TimeProvider _timeProvider;

    public RegisterLatePaymentEventHandler(
        IEconomicContractRepository contractRepo,
        IEconomicEventRepository eventRepo,
        TimeProvider timeProvider)
    {
        _contractRepo = contractRepo;
        _eventRepo = eventRepo;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterLatePaymentEventResponse> Handle(RegisterLatePaymentEventCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var contractId = EconomicContractId.From(request.ContractId);
        var baseCommitmentId = CommitmentId.From(request.CommitmentId);
        var currency = Enumeration.FromDisplayName<Currency>(request.Currency);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var contract = await _contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken)
            ?? throw EconomicContractErrors.ContractNotFound(request.ContractId);

        // Anti-duplicata: não cobre um commitment já coberto por outro pagamento.
        var existingCoverage = await _eventRepo.FindCoveredByCommitmentAsync(baseCommitmentId, tenantId, cancellationToken);
        if (existingCoverage is not null)
            throw EconomicContractErrors.CommitmentAlreadyCovered(request.CommitmentId);

        // Toda a política (atraso, valor da penalidade, total = base + penalidade, reuso) vive no agregado.
        var settlement = contract.MaterializeLatePaymentPenalty(
            baseCommitmentId, request.OccurredAt, request.TotalAmount, NewCommitmentId, now);

        // Penalty reusada de um relay anterior pode já ter sido coberta por outro evento.
        if (!settlement.PenaltyMaterialized)
        {
            var penaltyCoverage = await _eventRepo.FindCoveredByCommitmentAsync(settlement.PenaltyCommitmentId, tenantId, cancellationToken);
            if (penaltyCoverage is not null)
                throw EconomicContractErrors.CommitmentAlreadyCovered(settlement.PenaltyCommitmentId.Value);
        }

        var competence = contract.ResolveBundledCompetence([settlement.BaseCommitmentId, settlement.PenaltyCommitmentId]);
        var createdBy = request.UserId is { } uid ? UserId.From(uid) : (UserId?)null;

        var paymentEvent = EconomicEvent.RegisterBundledPayment(
            EconomicEventId.New(),
            tenantId,
            EconomicResourceId.New(),
            currency,
            request.OccurredAt,
            payerAgentId: EconomicAgentId.From(tenantId.Value),
            payeeAgentId: contract.CounterpartyId,
            lines:
            [
                new BundledPaymentLine(contract.Id.Value, settlement.BaseCommitmentId.Value, settlement.BaseAmountValue),
                new BundledPaymentLine(contract.Id.Value, settlement.PenaltyCommitmentId.Value, settlement.PenaltyAmountValue),
            ],
            competenceYear: competence.Year,
            competenceMonth: competence.Month,
            createdBy: createdBy,
            registeredAt: now);

        await _eventRepo.InsertAsync(paymentEvent, cancellationToken);

        // Transação multi-aggregate sancionada (IMultiAggregateCommand): o contrato tracked (trilha Penalty) e o
        // evento de caixa comitam num único SaveEntitiesAsync — junto com outbox e marca de idempotência.
        await _eventRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterLatePaymentEventResponse(
            paymentEvent.Id.Value,
            paymentEvent.Amount.Amount,
            settlement.BaseAmountValue,
            settlement.PenaltyAmountValue,
            settlement.PenaltyCommitmentId.Value,
            paymentEvent.Allocations.Count);
    }

    private static CommitmentId NewCommitmentId() => CommitmentId.From(Guid.CreateVersion7());
}

internal sealed class RegisterLatePaymentEventIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RegisterLatePaymentEventIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RegisterLatePaymentEventCommand, RegisterLatePaymentEventResponse>(mediator, requestManager, logger)
{
    protected override RegisterLatePaymentEventResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, 0m, 0m, 0m, Guid.Empty, 0);
}
