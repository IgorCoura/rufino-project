namespace EconomicCore.Application.Commands.RegisterPaymentEvent;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.Services;
using EconomicCore.Domain.SharedKernel;
using MediatR;

internal sealed class RegisterPaymentEventHandler : IRequestHandler<RegisterPaymentEventCommand, RegisterPaymentEventResponse>
{
    private readonly IEconomicContractRepository _contractRepo;
    private readonly IEconomicEventRepository _eventRepo;
    private readonly TimeProvider _timeProvider;

    public RegisterPaymentEventHandler(
        IEconomicContractRepository contractRepo,
        IEconomicEventRepository eventRepo,
        TimeProvider timeProvider)
    {
        _contractRepo = contractRepo;
        _eventRepo = eventRepo;
        _timeProvider = timeProvider;
    }

    public async Task<RegisterPaymentEventResponse> Handle(RegisterPaymentEventCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var contractId = EconomicContractId.From(request.ContractId);
        var commitmentId = CommitmentId.From(request.CommitmentId);
        var currency = Enumeration.FromDisplayName<Currency>(request.Currency);
        var paymentAmount = new Money(request.Amount, currency);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (request.OccurredAt > now)
            throw EconomicEventErrors.FuturePaidDate(request.OccurredAt, now);

        var contract = await _contractRepo.GetByIdAsync(contractId, tenantId, cancellationToken)
            ?? throw EconomicContractErrors.ContractNotFound(request.ContractId);

        if (!ReferenceEquals(contract.Status, ContractStatus.Active))
            throw EconomicContractErrors.ContractNotActive(contract.Status.Name);

        var outflowCommitment = contract.FindCommitment(commitmentId);
        if (outflowCommitment.Direction != CommitmentDirection.OutflowPromise)
            throw EconomicContractErrors.NoCoveringCommitmentForPeriod(
                outflowCommitment.Period.Year,
                outflowCommitment.Period.Month,
                CommitmentDirection.OutflowPromise.Name);

        if (outflowCommitment.Status != CommitmentStatus.Promised && outflowCommitment.Status != CommitmentStatus.Reserved)
            throw EconomicContractErrors.CannotFulfillInStatus(outflowCommitment.Status.Name);

        var existingCoverage = await _eventRepo.FindCoveredByCommitmentAsync(outflowCommitment.Id, tenantId, cancellationToken);
        if (existingCoverage is not null)
            throw EconomicContractErrors.CannotFulfillInStatus(outflowCommitment.Status.Name);

        if (paymentAmount.Amount != outflowCommitment.ExpectedAmount.Amount)
            throw EconomicContractErrors.PaymentAmountMismatch(
                outflowCommitment.ExpectedAmount.Amount,
                paymentAmount.Amount);

        var inflowCommitment = contract.FindReciprocalCommitment(outflowCommitment.Id);
        var period = new CompetencePeriod(outflowCommitment.Period.Year, outflowCommitment.Period.Month);

        var cashResourceId = EconomicResourceId.New();

        var participations = new List<Participation>
        {
            new(EconomicAgentId.From(tenantId.Value), ParticipationRole.Provider),
            new(contract.CounterpartyId, ParticipationRole.Recipient),
        };

        var createdBy = request.UserId is { } uid ? UserId.From(uid) : (UserId?)null;

        var paymentEvent = EconomicEvent.RegisterCovered(
            EconomicEventId.New(),
            tenantId,
            FlowDirection.Outflow,
            cashResourceId,
            paymentAmount,
            new EventTimestamp(request.OccurredAt),
            typeId: null,
            participations,
            new CommitmentRef(outflowCommitment.Id),
            period,
            createdBy: createdBy,
            registeredAt: now);

        await _eventRepo.InsertAsync(paymentEvent, cancellationToken);

        var consumptionEvent = await _eventRepo.FindCoveredByCommitmentAsync(
            inflowCommitment.Id, tenantId, cancellationToken);

        if (consumptionEvent is not null)
        {
            DualityMatchingService.Match(paymentEvent, consumptionEvent, request.OccurredAt);
            contract.MarkFulfilled(outflowCommitment.Id, paymentEvent.Id, request.OccurredAt);
            contract.MarkFulfilled(inflowCommitment.Id, consumptionEvent.Id, request.OccurredAt);
        }

        await _eventRepo.UnitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterPaymentEventResponse(
            paymentEvent.Id.Value,
            paymentEvent.Direction.Name,
            "Cash");
    }
}
