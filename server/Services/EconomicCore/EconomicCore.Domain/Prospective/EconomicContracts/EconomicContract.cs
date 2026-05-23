namespace EconomicCore.Domain.Prospective.EconomicContracts;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Prospective.EconomicContracts.Entities;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.Events;
using EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed class EconomicContract : AggregateRoot<EconomicContractId>
{
    private readonly List<Commitment> _commitments = [];

    public TenantId TenantId { get; private set; }
    public EconomicAgentId CounterpartyId { get; private set; }
    public ContractDirection Direction { get; private set; } = default!;
    public RecurrencePattern Recurrence { get; private set; } = default!;
    public CommitmentTerms DefaultTerms { get; private set; } = default!;
    public ContractStatus Status { get; private set; } = default!;
    public IReadOnlyCollection<Commitment> Commitments => _commitments.AsReadOnly();

    private EconomicContract() : base() { }
    private EconomicContract(EconomicContractId id) : base(id) { }

    public static EconomicContract Create(
        EconomicContractId id,
        TenantId tenantId,
        EconomicAgentId counterpartyId,
        ContractDirection direction,
        RecurrencePattern recurrence,
        CommitmentTerms defaultTerms,
        DateTime occurredAt)
    {
        var contract = new EconomicContract(id)
        {
            TenantId = tenantId,
            CounterpartyId = counterpartyId,
            Direction = direction,
            Recurrence = recurrence,
            DefaultTerms = defaultTerms,
            Status = ContractStatus.Active,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

        contract.AddDomainEvent(new EconomicContractCreated(
            EventId: Guid.NewGuid(),
            ContractId: contract.Id,
            TenantId: contract.TenantId,
            CounterpartyId: contract.CounterpartyId,
            DirectionName: contract.Direction.Name,
            PeriodicityName: contract.Recurrence.Periodicity.Name,
            AnchorDay: contract.Recurrence.AnchorDay,
            ExpectedAmountValue: contract.DefaultTerms.ExpectedAmount.Amount,
            ExpectedAmountCurrency: contract.DefaultTerms.ExpectedAmount.Currency.Name,
            TolerancePercent: contract.DefaultTerms.TolerancePercent,
            WindowDays: contract.DefaultTerms.WindowDays,
            OccurredAt: occurredAt));

        return contract;
    }

    /// <summary>
    /// Generates the reciprocal pair of commitments for the given period (outflow + inflow).
    /// CTR01 is structural: pair is always generated together with reciprocal links to each other.
    /// CTR02 prevents duplicates per (period, direction).
    /// </summary>
    public void GenerateCommitmentsFor(
        CompetencePeriod period,
        CommitmentId outflowCommitmentId,
        CommitmentId inflowCommitmentId,
        DateTime occurredAt)
    {
        if (!ReferenceEquals(Status, ContractStatus.Active))
            throw EconomicContractErrors.ContractNotActive(Status.Name);

        if (_commitments.Any(c => c.Period.Equals(period) && c.Direction == CommitmentDirection.OutflowPromise))
            throw EconomicContractErrors.DuplicateCommitmentForPeriod(period.Year, period.Month, CommitmentDirection.OutflowPromise.Name);
        if (_commitments.Any(c => c.Period.Equals(period) && c.Direction == CommitmentDirection.InflowPromise))
            throw EconomicContractErrors.DuplicateCommitmentForPeriod(period.Year, period.Month, CommitmentDirection.InflowPromise.Name);

        var window = BuildFulfillmentWindow(period);
        var amount = DefaultTerms.ExpectedAmount;

        var outflow = new Commitment(outflowCommitmentId, CommitmentDirection.OutflowPromise, period, amount, window, occurredAt);
        var inflow = new Commitment(inflowCommitmentId, CommitmentDirection.InflowPromise, period, amount, window, occurredAt);
        outflow.SetReciprocal(new ReciprocalLink(inflow.Id));
        inflow.SetReciprocal(new ReciprocalLink(outflow.Id));

        if (outflow.Reciprocal is null)
            throw EconomicContractErrors.MissingReciprocal();

        _commitments.Add(outflow);
        _commitments.Add(inflow);
        UpdatedAt = occurredAt;

        AddDomainEvent(new CommitmentsGenerated(
            EventId: Guid.NewGuid(),
            ContractId: Id,
            TenantId: TenantId,
            PeriodYear: period.Year,
            PeriodMonth: period.Month,
            OutflowCommitmentId: outflow.Id,
            InflowCommitmentId: inflow.Id,
            ExpectedAmountValue: amount.Amount,
            ExpectedAmountCurrency: amount.Currency.Name,
            OccurredAt: occurredAt));
    }

    public void MarkFulfilled(CommitmentId commitmentId, EconomicEventId fulfillingEventId, DateTime occurredAt)
    {
        var commitment = FindCommitmentOrThrow(commitmentId);
        commitment.MarkFulfilledBy(fulfillingEventId, occurredAt);
        UpdatedAt = occurredAt;

        AddDomainEvent(new CommitmentFulfilled(
            EventId: Guid.NewGuid(),
            ContractId: Id,
            CommitmentId: commitment.Id,
            FulfillingEventId: fulfillingEventId,
            OccurredAt: occurredAt));
    }

    public void Expire(CommitmentId commitmentId, DateTime occurredAt)
    {
        var commitment = FindCommitmentOrThrow(commitmentId);
        commitment.MarkExpired(occurredAt);
        UpdatedAt = occurredAt;

        AddDomainEvent(new CommitmentExpired(
            EventId: Guid.NewGuid(),
            ContractId: Id,
            CommitmentId: commitment.Id,
            OccurredAt: occurredAt));
    }

    public void CancelCommitment(CommitmentId commitmentId, DateTime occurredAt)
    {
        var commitment = FindCommitmentOrThrow(commitmentId);
        commitment.MarkCancelled(occurredAt);
        UpdatedAt = occurredAt;

        AddDomainEvent(new CommitmentCancelled(
            EventId: Guid.NewGuid(),
            ContractId: Id,
            CommitmentId: commitment.Id,
            OccurredAt: occurredAt));
    }

    public void Suspend(DateTime occurredAt) => TransitionStatusTo(ContractStatus.Suspended, occurredAt);

    public void Resume(DateTime occurredAt) => TransitionStatusTo(ContractStatus.Active, occurredAt);

    public void Terminate(DateTime occurredAt) => TransitionStatusTo(ContractStatus.Terminated, occurredAt);

    private void TransitionStatusTo(ContractStatus target, DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(target))
            throw EconomicContractErrors.InvalidContractStatusTransition(Status.Name, target.Name);

        Status = target;
        UpdatedAt = occurredAt;
    }

    private Commitment FindCommitmentOrThrow(CommitmentId commitmentId)
    {
        var commitment = _commitments.FirstOrDefault(c => c.Id.Equals(commitmentId));
        if (commitment is null)
            throw EconomicContractErrors.CommitmentNotFound(commitmentId.Value);
        return commitment;
    }

    private DateRange BuildFulfillmentWindow(CompetencePeriod period)
    {
        var anchor = Math.Min(Recurrence.AnchorDay, DateTime.DaysInMonth(period.Year, period.Month));
        var from = new DateOnly(period.Year, period.Month, anchor);
        var to = from.AddDays(DefaultTerms.WindowDays);
        return new DateRange(from, to);
    }
}
