namespace EconomicCore.Domain.Prospective.EconomicContracts;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts.Entities;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.Events;
using EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed class EconomicContract : AggregateRoot<EconomicContractId>
{
    public const int MIN_TERM_MONTHS = 1;
    public const int MAX_TERM_MONTHS = 120;
    public const int MAX_START_DATE_PAST_YEARS = 1;

    private readonly List<Commitment> _commitments = [];

    public TenantId TenantId { get; private set; }
    public EconomicAgentId CounterpartyId { get; private set; }
    public EconomicResourceId ResourceId { get; private set; }
    public ContractDirection Direction { get; private set; } = default!;
    public RecurrencePattern Recurrence { get; private set; } = default!;
    public CommitmentTerms DefaultTerms { get; private set; } = default!;
    public int TermMonths { get; private set; }
    public DateOnly StartDate { get; private set; }
    public ContractStatus Status { get; private set; } = default!;
    public IReadOnlyCollection<Commitment> Commitments => _commitments.AsReadOnly();

    private EconomicContract() : base() { }
    private EconomicContract(EconomicContractId id) : base(id) { }

    public static EconomicContract Create(
        EconomicContractId id,
        TenantId tenantId,
        EconomicAgentId counterpartyId,
        EconomicResourceId resourceId,
        ContractDirection direction,
        RecurrencePattern recurrence,
        CommitmentTerms defaultTerms,
        int termMonths,
        DateOnly startDate,
        DateTime occurredAt)
    {
        var contract = new EconomicContract(id)
        {
            TenantId = tenantId,
            CounterpartyId = counterpartyId,
            ResourceId = resourceId,
            Direction = direction,
            Recurrence = recurrence,
            DefaultTerms = defaultTerms,
            Status = ContractStatus.Draft,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };
        contract.SetTermMonths(termMonths);
        contract.SetStartDate(startDate, occurredAt);

        contract.AddDomainEvent(new EconomicContractCreated(
            EventId: Guid.NewGuid(),
            ContractId: contract.Id,
            TenantId: contract.TenantId,
            CounterpartyId: contract.CounterpartyId,
            ResourceId: contract.ResourceId,
            DirectionName: contract.Direction.Name,
            PeriodicityName: contract.Recurrence.Periodicity.Name,
            AnchorDay: contract.Recurrence.AnchorDay,
            TermMonths: contract.TermMonths,
            StartDate: contract.StartDate,
            ExpectedAmountValue: contract.DefaultTerms.ExpectedAmount.Amount,
            ExpectedAmountCurrency: contract.DefaultTerms.ExpectedAmount.Currency.Name,
            TolerancePercent: contract.DefaultTerms.TolerancePercent,
            WindowDays: contract.DefaultTerms.WindowDays,
            OccurredAt: occurredAt));

        return contract;
    }

    /// <summary>
    /// Activates the contract: materializes the full term as TermMonths × (outflow + inflow)
    /// reciprocal commitment pairs in a single atomic operation.
    /// CTR16 guards Draft-only activation. The status transitions Draft → Active at the end.
    /// </summary>
    public void Activate(DateTime occurredAt, Func<CommitmentId> commitmentIdFactory)
    {
        if (!ReferenceEquals(Status, ContractStatus.Draft))
            throw EconomicContractErrors.ContractNotDraft(Status.Name);

        for (var n = 0; n < TermMonths; n++)
        {
            var monthDate = StartDate.AddMonths(n);
            var period = new CompetencePeriod(monthDate.Year, monthDate.Month);
            GenerateCommitmentsFor(period, commitmentIdFactory(), commitmentIdFactory(), occurredAt);
        }

        TransitionStatusTo(ContractStatus.Active, occurredAt);

        AddDomainEvent(new ContractActivated(
            EventId: Guid.NewGuid(),
            ContractId: Id,
            TenantId: TenantId,
            TermMonths: TermMonths,
            OccurredAt: occurredAt));
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
        if (!ReferenceEquals(Status, ContractStatus.Active) && !ReferenceEquals(Status, ContractStatus.Draft))
            throw EconomicContractErrors.ContractNotActive(Status.Name);

        if (_commitments.Any(c => c.Period.Equals(period) && c.Direction == CommitmentDirection.OutflowPromise))
            throw EconomicContractErrors.DuplicateCommitmentForPeriod(period.Year, period.Month, CommitmentDirection.OutflowPromise.Name);
        if (_commitments.Any(c => c.Period.Equals(period) && c.Direction == CommitmentDirection.InflowPromise))
            throw EconomicContractErrors.DuplicateCommitmentForPeriod(period.Year, period.Month, CommitmentDirection.InflowPromise.Name);

        var window = BuildFulfillmentWindow(period);
        var outflowAmount = new Money(DefaultTerms.ExpectedAmount.Amount, DefaultTerms.ExpectedAmount.Currency);
        var inflowAmount = new Money(DefaultTerms.ExpectedAmount.Amount, DefaultTerms.ExpectedAmount.Currency);
        var outflowPeriod = new CompetencePeriod(period.Year, period.Month);
        var inflowPeriod = new CompetencePeriod(period.Year, period.Month);
        var outflowWindow = new DateRange(window.From, window.To);
        var inflowWindow = new DateRange(window.From, window.To);

        var outflow = new Commitment(outflowCommitmentId, CommitmentDirection.OutflowPromise, outflowPeriod, outflowAmount, outflowWindow, occurredAt);
        var inflow = new Commitment(inflowCommitmentId, CommitmentDirection.InflowPromise, inflowPeriod, inflowAmount, inflowWindow, occurredAt);
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
            ExpectedAmountValue: DefaultTerms.ExpectedAmount.Amount,
            ExpectedAmountCurrency: DefaultTerms.ExpectedAmount.Currency.Name,
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

    public void Terminate(DateTime occurredAt)
    {
        TransitionStatusTo(ContractStatus.Terminated, occurredAt);
        AddDomainEvent(new ContractTerminated(
            EventId: Guid.NewGuid(),
            ContractId: Id,
            TenantId: TenantId,
            OccurredAt: occurredAt));
    }

    private void TransitionStatusTo(ContractStatus target, DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(target))
            throw EconomicContractErrors.InvalidContractStatusTransition(Status.Name, target.Name);

        Status = target;
        UpdatedAt = occurredAt;
    }

    public Commitment FindPromisedCommitment(CompetencePeriod period, CommitmentDirection direction)
    {
        var commitment = _commitments
            .FirstOrDefault(c => c.Period.Equals(period)
                && c.Direction == direction
                && c.Status == CommitmentStatus.Promised);

        if (commitment is null)
            throw EconomicContractErrors.NoCoveringCommitmentForPeriod(
                period.Year, period.Month, direction.Name);

        return commitment;
    }

    public Commitment FindReciprocalCommitment(CommitmentId commitmentId)
    {
        var commitment = FindCommitmentOrThrow(commitmentId);
        if (commitment.Reciprocal is null)
            throw EconomicContractErrors.MissingReciprocal();

        return FindCommitmentOrThrow(commitment.Reciprocal.ReciprocalCommitmentId);
    }

    public Commitment FindCommitment(CommitmentId commitmentId) => FindCommitmentOrThrow(commitmentId);

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

    private void SetTermMonths(int termMonths)
    {
        if (termMonths < MIN_TERM_MONTHS || termMonths > MAX_TERM_MONTHS)
            throw EconomicContractErrors.InvalidTermMonths(termMonths);

        TermMonths = termMonths;
    }

    private void SetStartDate(DateOnly startDate, DateTime occurredAt)
    {
        var earliest = DateOnly.FromDateTime(occurredAt).AddYears(-MAX_START_DATE_PAST_YEARS);
        if (startDate < earliest)
            throw EconomicContractErrors.InvalidStartDate(startDate);

        StartDate = startDate;
    }
}
