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
    public const decimal DEFAULT_TOLERANCE_PERCENT = 0m;
    public const int DEFAULT_WINDOW_DAYS = 30;
    public const decimal DEFAULT_FINE_PERCENT = 0.02m;
    public const decimal DEFAULT_MONTHLY_INTEREST_PERCENT = 0.01m;

    private readonly List<Commitment> _commitments = [];
    private readonly List<ContractCharge> _charges = [];

    public TenantId TenantId { get; private set; }
    public EconomicAgentId CounterpartyId { get; private set; }
    public EconomicResourceId ResourceId { get; private set; }
    public ContractDirection Direction { get; private set; } = default!;
    public RecurrencePattern Recurrence { get; private set; } = default!;
    public CommitmentTerms DefaultTerms { get; private set; } = default!;
    public PenaltyTerms PenaltyPolicy { get; private set; } = default!;

    /// <summary>
    /// Purpose of the contract's core track (DefaultTerms). Rent for a lease; Insurance for a standalone insurance
    /// contract (counterparty = insurer); PropertyTax for an IPTU fiscal obligation (counterparty = municipality).
    /// Additional <see cref="Charges"/> ride alongside it.
    /// </summary>
    public CommitmentPurpose PrimaryPurpose { get; private set; } = default!;
    public int TermMonths { get; private set; }
    public DateOnly StartDate { get; private set; }
    public ContractStatus Status { get; private set; } = default!;
    public IReadOnlyCollection<Commitment> Commitments => _commitments.AsReadOnly();

    /// <summary>
    /// Additional recurring charge tracks (condominium, property tax, insurance) bundled into this contract.
    /// The Rent track is the contract core (DefaultTerms) and is NOT represented here. On activation each charge
    /// yields its own reciprocal commitment pair per period, tagged with the charge's <see cref="CommitmentPurpose"/>.
    /// </summary>
    public IReadOnlyCollection<ContractCharge> Charges => _charges.AsReadOnly();

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
        DateTime occurredAt,
        CommitmentPurpose? primaryPurpose = null)
    {
        var contract = new EconomicContract(id)
        {
            TenantId = tenantId,
            CounterpartyId = counterpartyId,
            ResourceId = resourceId,
            Direction = direction,
            Recurrence = recurrence,
            DefaultTerms = defaultTerms,
            PenaltyPolicy = new PenaltyTerms(DEFAULT_FINE_PERCENT, DEFAULT_MONTHLY_INTEREST_PERCENT),
            PrimaryPurpose = primaryPurpose ?? CommitmentPurpose.Rent,
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
    /// Factory from primitive inputs: composes the contract's Value Objects (RecurrencePattern, Money,
    /// CommitmentTerms) internally so callers (Application) never assemble domain types. The fulfillment
    /// tolerance/window use the contract defaults. Delegates to the canonical Value-Object factory.
    /// </summary>
    public static EconomicContract Create(
        EconomicContractId id,
        TenantId tenantId,
        EconomicAgentId counterpartyId,
        EconomicResourceId resourceId,
        ContractDirection direction,
        Periodicity periodicity,
        int anchorDay,
        decimal expectedAmountValue,
        Currency currency,
        int termMonths,
        DateOnly startDate,
        DateTime occurredAt,
        CommitmentPurpose? primaryPurpose = null)
    {
        var recurrence = new RecurrencePattern(periodicity, anchorDay);
        var defaultTerms = new CommitmentTerms(
            new Money(expectedAmountValue, currency),
            DEFAULT_TOLERANCE_PERCENT,
            DEFAULT_WINDOW_DAYS);

        return Create(id, tenantId, counterpartyId, resourceId, direction, recurrence, defaultTerms, termMonths, startDate, occurredAt, primaryPurpose);
    }

    /// <summary>
    /// Adds an additional recurring charge track (condominium, property tax, insurance) to a Draft contract.
    /// Only allowed before activation (CTR22), rejects the Rent purpose (it is the implicit core track — CTR24),
    /// and forbids duplicate purposes (CTR23). On <see cref="Activate"/> each charge materializes its own
    /// reciprocal commitment pair per period.
    /// </summary>
    public void AddCharge(
        CommitmentPurpose purpose,
        decimal expectedAmount,
        Currency currency,
        EconomicResourceId resourceId,
        EconomicAgentId recipientAgentId,
        bool collectedByCounterparty,
        DateTime occurredAt)
    {
        if (!ReferenceEquals(Status, ContractStatus.Draft))
            throw EconomicContractErrors.ChargesOnlyInDraft(Status.Name);
        if (purpose == PrimaryPurpose)
            throw EconomicContractErrors.RentChargeImplicit();
        if (_charges.Any(c => c.Purpose == purpose))
            throw EconomicContractErrors.DuplicateChargePurpose(purpose.Name);

        _charges.Add(new ContractCharge(purpose, new Money(expectedAmount, currency), resourceId, recipientAgentId, collectedByCounterparty));
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Activates the contract: materializes the full term as TermMonths × (1 + Charges.Count) reciprocal
    /// commitment pairs in a single atomic operation — the Rent core track plus one track per additional charge.
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

            GeneratePair(period, PrimaryPurpose, DefaultTerms.ExpectedAmount,
                commitmentIdFactory(), commitmentIdFactory(), occurredAt);

            foreach (var charge in _charges)
                GeneratePair(period, charge.Purpose, charge.ExpectedAmount,
                    commitmentIdFactory(), commitmentIdFactory(), occurredAt);
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
    /// Generates the Rent reciprocal pair of commitments for the given period (outflow + inflow). Used by the
    /// recurring scheduler. CTR01 is structural: pair is always generated together with reciprocal links to each
    /// other. CTR02 prevents duplicates per (period, direction, purpose).
    /// </summary>
    public void GenerateCommitmentsFor(
        CompetencePeriod period,
        CommitmentId outflowCommitmentId,
        CommitmentId inflowCommitmentId,
        DateTime occurredAt)
    {
        if (!ReferenceEquals(Status, ContractStatus.Active) && !ReferenceEquals(Status, ContractStatus.Draft))
            throw EconomicContractErrors.ContractNotActive(Status.Name);

        GeneratePair(period, PrimaryPurpose, DefaultTerms.ExpectedAmount,
            outflowCommitmentId, inflowCommitmentId, occurredAt);
    }

    /// <summary>
    /// Materializes a single reciprocal pair (outflow + inflow) for a (period, purpose) track at the given amount.
    /// CTR02 prevents duplicates per (period, direction, purpose) — multiple charge tracks coexist in one period.
    /// </summary>
    private void GeneratePair(
        CompetencePeriod period,
        CommitmentPurpose purpose,
        Money amount,
        CommitmentId outflowCommitmentId,
        CommitmentId inflowCommitmentId,
        DateTime occurredAt)
    {
        if (_commitments.Any(c => c.Period.Equals(period) && c.Direction == CommitmentDirection.OutflowPromise && c.Purpose == purpose))
            throw EconomicContractErrors.DuplicateCommitmentForPeriod(period.Year, period.Month, CommitmentDirection.OutflowPromise.Name);
        if (_commitments.Any(c => c.Period.Equals(period) && c.Direction == CommitmentDirection.InflowPromise && c.Purpose == purpose))
            throw EconomicContractErrors.DuplicateCommitmentForPeriod(period.Year, period.Month, CommitmentDirection.InflowPromise.Name);

        var window = BuildFulfillmentWindow(period);
        var outflowAmount = new Money(amount.Amount, amount.Currency);
        var inflowAmount = new Money(amount.Amount, amount.Currency);
        var outflowPeriod = new CompetencePeriod(period.Year, period.Month);
        var inflowPeriod = new CompetencePeriod(period.Year, period.Month);
        var outflowWindow = new DateRange(window.From, window.To);
        var inflowWindow = new DateRange(window.From, window.To);

        var outflow = new Commitment(outflowCommitmentId, CommitmentDirection.OutflowPromise, purpose, outflowPeriod, outflowAmount, outflowWindow, occurredAt);
        var inflow = new Commitment(inflowCommitmentId, CommitmentDirection.InflowPromise, purpose, inflowPeriod, inflowAmount, inflowWindow, occurredAt);
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
            Purpose: purpose.Name,
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

    /// <summary>
    /// Materializes a late-payment penalty (multa + juros de mora) as a reciprocal Penalty pair when the commitment
    /// <paramref name="paidCommitmentId"/> was settled after its fulfillment window. The Penalty obligation is born
    /// on breach (Hruby §10.5), never pre-generated. Idempotent: returns false (no-op) when the payment was on time
    /// or when a Penalty track already exists for that period. The amount applies <see cref="PenaltyPolicy"/> to the
    /// original commitment amount over the number of full months late.
    /// </summary>
    public bool TryRegisterLatePenalty(
        CommitmentId paidCommitmentId,
        DateOnly paidDate,
        Func<CommitmentId> penaltyIdFactory,
        DateTime occurredAt)
    {
        var paid = FindCommitmentOrThrow(paidCommitmentId);

        // A decisão de quem é penalizável é do agregado: só o pagamento (outflow) de uma trilha não-Penalty.
        // Chamadas para a perna de inflow (ou para a própria trilha Penalty) são no-op — o relay pode chamar
        // este método para os dois lados da duality sem precisar saber qual é qual.
        if (paid.Direction != CommitmentDirection.OutflowPromise || paid.Purpose == CommitmentPurpose.Penalty)
            return false;

        var dueDate = paid.FulfillmentWindow.To;
        if (paidDate <= dueDate)
            return false;

        // Idempotent: a reprocessed relay message must not materialize a second penalty for the same period.
        if (_commitments.Any(c => c.Purpose == CommitmentPurpose.Penalty && c.Period.Equals(paid.Period)))
            return false;

        var monthsLate = ((paidDate.Year * 12) + paidDate.Month) - ((dueDate.Year * 12) + dueDate.Month);
        var penaltyAmount = PenaltyPolicy.ComputePenalty(paid.ExpectedAmount, monthsLate);
        if (!penaltyAmount.IsPositive)
            return false;

        GeneratePair(paid.Period, CommitmentPurpose.Penalty, penaltyAmount, penaltyIdFactory(), penaltyIdFactory(), occurredAt);
        return true;
    }

    /// <summary>
    /// Pre-condition gate (no mutation) for covering an outflow commitment with a payment: contract must be
    /// Active, the commitment must be an OutflowPromise still open (Promised/Reserved), and the paid amount
    /// must match the expected amount exactly (partial payment unsupported — CTR19).
    /// </summary>
    public void EnsurePayable(CommitmentId commitmentId, decimal paymentAmountValue)
    {
        EnsureActive();
        var commitment = FindCommitmentOrThrow(commitmentId);
        EnsureDirection(commitment, CommitmentDirection.OutflowPromise);
        EnsureOpenForCoverage(commitment);

        if (paymentAmountValue != commitment.ExpectedAmount.Amount)
            throw EconomicContractErrors.PaymentAmountMismatch(commitment.ExpectedAmount.Amount, paymentAmountValue);
    }

    /// <summary>
    /// Pre-condition gate (no mutation) for covering an inflow commitment with an occupancy/consumption: contract
    /// must be Active, the commitment must be an InflowPromise still open (Promised/Reserved), and the competence
    /// month must have already started (no occupancy registered for a future month — EVT14).
    /// </summary>
    public void EnsureOccupiable(CommitmentId commitmentId, DateOnly today)
    {
        EnsureActive();
        var commitment = FindCommitmentOrThrow(commitmentId);
        EnsureDirection(commitment, CommitmentDirection.InflowPromise);
        EnsureOpenForCoverage(commitment);

        if (commitment.Period.FirstDay() > today)
            throw EconomicEventErrors.OccupancyInFuture(commitment.Period.Year, commitment.Period.Month, today);
    }

    private void EnsureActive()
    {
        if (!ReferenceEquals(Status, ContractStatus.Active))
            throw EconomicContractErrors.ContractNotActive(Status.Name);
    }

    private static void EnsureDirection(Commitment commitment, CommitmentDirection expected)
    {
        if (commitment.Direction != expected)
            throw EconomicContractErrors.NoCoveringCommitmentForPeriod(
                commitment.Period.Year, commitment.Period.Month, expected.Name);
    }

    private static void EnsureOpenForCoverage(Commitment commitment)
    {
        if (commitment.Status != CommitmentStatus.Promised && commitment.Status != CommitmentStatus.Reserved)
            throw EconomicContractErrors.CannotFulfillInStatus(commitment.Status.Name);
    }

    public void Suspend(DateTime occurredAt) => TransitionStatusTo(ContractStatus.Suspended, occurredAt);

    public void Resume(DateTime occurredAt) => TransitionStatusTo(ContractStatus.Active, occurredAt);

    /// <summary>
    /// Mid-term adjustment (reajuste): re-prices every still-open (Promised) commitment of the given track from
    /// <paramref name="effectiveFrom"/> onward to <paramref name="newAmount"/>. Commitments already settled in or
    /// after that competence are locked and block the operation (CTR40, Value Pattern LockValue). CTR41 if there is
    /// nothing open to re-price. The caller composes the new amount (absolute or index-applied).
    /// </summary>
    public Money ApplyAdjustmentToAmount(
        CommitmentPurpose purpose, int effectiveFromYear, int effectiveFromMonth, decimal newValue, Currency currency, DateTime occurredAt)
        => ApplyAdjustmentCore(purpose, new CompetencePeriod(effectiveFromYear, effectiveFromMonth), new Money(newValue, currency), occurredAt);

    /// <summary>
    /// Mid-term adjustment by an index rate (e.g. IGPM 0.06): the new amount is the track's current amount times
    /// (1 + rate). The pricing rule and the Money composition live here, in the aggregate — never in the caller.
    /// </summary>
    public Money ApplyAdjustmentByRate(
        CommitmentPurpose purpose, int effectiveFromYear, int effectiveFromMonth, decimal indexRate, DateTime occurredAt)
    {
        var newAmount = CurrentTrackAmount(purpose).Multiply(1m + indexRate);
        return ApplyAdjustmentCore(purpose, new CompetencePeriod(effectiveFromYear, effectiveFromMonth), newAmount, occurredAt);
    }

    /// <summary>
    /// Re-prices every still-open (Promised) commitment of the track from <paramref name="effectiveFrom"/> onward to
    /// <paramref name="newAmount"/>. Commitments already settled in/after that competence are locked and block the
    /// operation (CTR40, Value Pattern LockValue). CTR41 when there is nothing open to re-price. Returns the applied amount.
    /// </summary>
    private Money ApplyAdjustmentCore(CommitmentPurpose purpose, CompetencePeriod effectiveFrom, Money newAmount, DateTime occurredAt)
    {
        EnsureActive();

        if (_commitments.Any(c => c.Purpose == purpose
            && c.Status == CommitmentStatus.Fulfilled
            && IsPeriodOnOrAfter(c.Period, effectiveFrom)))
            throw EconomicContractErrors.AdjustmentOverLockedPeriod(effectiveFrom.Year, effectiveFrom.Month);

        var targets = _commitments
            .Where(c => c.Purpose == purpose
                && c.Status == CommitmentStatus.Promised
                && IsPeriodOnOrAfter(c.Period, effectiveFrom))
            .ToList();
        if (targets.Count == 0)
            throw EconomicContractErrors.NoCommitmentsToAdjust(purpose.Name, effectiveFrom.Year, effectiveFrom.Month);

        foreach (var commitment in targets)
            commitment.Reprice(new Money(newAmount.Amount, newAmount.Currency), occurredAt);

        UpdatedAt = occurredAt;
        AddDomainEvent(new ContractAdjusted(
            EventId: Guid.NewGuid(),
            ContractId: Id,
            TenantId: TenantId,
            Purpose: purpose.Name,
            EffectiveFromYear: effectiveFrom.Year,
            EffectiveFromMonth: effectiveFrom.Month,
            NewAmountValue: newAmount.Amount,
            NewAmountCurrency: newAmount.Currency.Name,
            RepricedCount: targets.Count,
            OccurredAt: occurredAt));

        return newAmount;
    }

    private static bool IsPeriodOnOrAfter(CompetencePeriod period, CompetencePeriod from)
        => period.Year > from.Year || (period.Year == from.Year && period.Month >= from.Month);

    private Money CurrentTrackAmount(CommitmentPurpose purpose)
    {
        var latest = _commitments
            .Where(c => c.Purpose == purpose)
            .OrderByDescending(c => c.Period.Year).ThenByDescending(c => c.Period.Month)
            .FirstOrDefault()
            ?? throw EconomicContractErrors.NoChargeTrack(purpose.Name);

        return latest.ExpectedAmount;
    }

    /// <summary>
    /// Terminates the contract on <paramref name="terminationDate"/>. Guards the status transition first
    /// (no side effects if it can't terminate), then rejects a date earlier than the last occupied inflow
    /// period (CTR20), then cancels in cascade every still-pending commitment whose period starts after the
    /// termination date, and finally transitions to Terminated. The caller resolves
    /// <paramref name="lastOccupiedInflowPeriod"/> (an I/O lookup over registered inflow events) and passes it in.
    /// </summary>
    public void Terminate(DateOnly terminationDate, CompetencePeriod? lastOccupiedInflowPeriod, DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(ContractStatus.Terminated))
            throw EconomicContractErrors.InvalidContractStatusTransition(Status.Name, ContractStatus.Terminated.Name);

        if (lastOccupiedInflowPeriod is not null && terminationDate < lastOccupiedInflowPeriod.LastDay())
            throw EconomicContractErrors.InvalidTerminationDate(
                terminationDate,
                $"{lastOccupiedInflowPeriod.Year:D4}-{lastOccupiedInflowPeriod.Month:D2}");

        foreach (var commitment in CancellableFutureCommitments(terminationDate))
        {
            commitment.MarkCancelled(occurredAt);
            AddDomainEvent(new CommitmentCancelled(
                EventId: Guid.NewGuid(),
                ContractId: Id,
                CommitmentId: commitment.Id,
                OccurredAt: occurredAt));
        }

        TransitionStatusTo(ContractStatus.Terminated, occurredAt);
        AddDomainEvent(new ContractTerminated(
            EventId: Guid.NewGuid(),
            ContractId: Id,
            TenantId: TenantId,
            OccurredAt: occurredAt));
    }

    private List<Commitment> CancellableFutureCommitments(DateOnly terminationDate)
        => _commitments
            .Where(c => (c.Status == CommitmentStatus.Promised || c.Status == CommitmentStatus.Reserved)
                && c.Period.FirstDay() > terminationDate)
            .ToList();

    private void TransitionStatusTo(ContractStatus target, DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(target))
            throw EconomicContractErrors.InvalidContractStatusTransition(Status.Name, target.Name);

        Status = target;
        UpdatedAt = occurredAt;
    }

    public Commitment FindPromisedCommitment(CompetencePeriod period, CommitmentDirection direction)
        => FindPromisedCommitment(period, direction, PrimaryPurpose);

    /// <summary>
    /// Finds the open (Promised) commitment for a specific (period, direction, purpose) track. With multiple
    /// charge tracks per period the purpose disambiguates which track (rent vs condominium vs …) to cover.
    /// </summary>
    public Commitment FindPromisedCommitment(CompetencePeriod period, CommitmentDirection direction, CommitmentPurpose purpose)
    {
        var commitment = _commitments
            .FirstOrDefault(c => c.Period.Equals(period)
                && c.Direction == direction
                && c.Purpose == purpose
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
