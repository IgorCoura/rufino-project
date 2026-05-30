namespace EconomicCore.Domain.Operational.EconomicEvents;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.Operational.EconomicEvents.Events;
using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed class EconomicEvent : AggregateRoot<EconomicEventId>
{
    private readonly List<Participation> _participations = [];
    private readonly List<PaymentAllocation> _allocations = [];
    private readonly List<DualityLink> _dualityLinks = [];

    public TenantId TenantId { get; private set; }
    public FlowDirection Direction { get; private set; } = default!;
    public EconomicResourceId ResourceId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public EventTimestamp OccurredAt { get; private set; } = default!;
    public EconomicEventTypeId? TypeId { get; private set; }
    public IReadOnlyCollection<Participation> Participations => _participations.AsReadOnly();

    /// <summary>Covering legs: which commitments this (possibly bundled) event settles, and for how much each.</summary>
    public IReadOnlyCollection<PaymentAllocation> Allocations => _allocations.AsReadOnly();

    /// <summary>Closed duality legs (one per matched counterpart). Grows as each covered allocation pairs up.</summary>
    public IReadOnlyCollection<DualityLink> DualityLinks => _dualityLinks.AsReadOnly();

    public CompetencePeriod Competence { get; private set; } = default!;
    public UserId? CreatedBy { get; private set; }

    private EconomicEvent() : base() { }
    private EconomicEvent(EconomicEventId id) : base(id) { }

    /// <summary>Total already matched against the allocation that covers <paramref name="commitmentId"/>.</summary>
    public decimal MatchedAmountFor(CommitmentId commitmentId)
        => _dualityLinks
            .Where(d => d.CommitmentId is { } c && c.Equals(commitmentId))
            .Sum(d => d.MatchedAmount.Amount);

    /// <summary>True when every covering allocation has been fully matched (the event's duality is fully closed).</summary>
    public bool IsFullyMatched
        => _allocations.Count > 0
            && _allocations.All(a => MatchedAmountFor(a.Commitment.CommitmentId) >= a.Amount.Amount);

    /// <summary>True when a duality leg already exists for the given covering commitment (idempotency guard).</summary>
    public bool HasClosedDualityFor(CommitmentId commitmentId)
        => _dualityLinks.Any(d => d.CommitmentId is { } c && c.Equals(commitmentId));

    /// <summary>
    /// Registers an event covered by a Commitment (post/pre-paid, first to arrive).
    /// The Duality is filled later by <see cref="CloseDuality"/> when the counterpart arrives.
    /// </summary>
    public static EconomicEvent RegisterCovered(
        EconomicEventId id,
        TenantId tenantId,
        FlowDirection direction,
        EconomicResourceId resourceId,
        Money amount,
        EventTimestamp occurredAt,
        EconomicEventTypeId? typeId,
        IReadOnlyCollection<Participation> participations,
        CommitmentRef coveringCommitment,
        CompetencePeriod competence,
        UserId? createdBy,
        DateTime registeredAt)
    {
        if (coveringCommitment is null)
            throw EconomicEventErrors.OrphanEvent();

        var ev = BuildBase(id, tenantId, direction, resourceId, amount, occurredAt, typeId, participations, competence, createdBy, registeredAt);
        ev._allocations.Add(new PaymentAllocation(coveringCommitment, new Money(amount.Amount, amount.Currency)));

        ev.AddDomainEvent(new EconomicEventRegistered(
            EventId: Guid.NewGuid(),
            EconomicEventId: ev.Id,
            TenantId: ev.TenantId,
            DirectionName: ev.Direction.Name,
            ResourceId: ev.ResourceId,
            AmountValue: ev.Amount.Amount,
            AmountCurrency: ev.Amount.Currency.Name,
            CompetenceYear: ev.Competence.Year,
            CompetenceMonth: ev.Competence.Month,
            Coverings: [new EconomicEventCovering(coveringCommitment.ContractId.Value, coveringCommitment.CommitmentId.Value)],
            CounterpartEventId: null,
            OccurredAt: occurredAt.InstantUtc));

        return ev;
    }

    /// <summary>
    /// Factory for a payment (cash Outflow) that covers an outflow commitment. Composes the event's Value Objects
    /// internally (Participations, Money, CompetencePeriod, CommitmentRef, EventTimestamp) so callers never assemble
    /// domain types nor share VO instances with the commitment. Rejects a paid date in the future (EVT15).
    /// </summary>
    public static EconomicEvent RegisterPaymentCoverage(
        EconomicEventId id,
        TenantId tenantId,
        EconomicResourceId cashResourceId,
        decimal amountValue,
        Currency currency,
        DateTime occurredAt,
        EconomicAgentId payerAgentId,
        EconomicAgentId payeeAgentId,
        EconomicContractId coveringContractId,
        CommitmentId coveringCommitmentId,
        int competenceYear,
        int competenceMonth,
        UserId? createdBy,
        DateTime registeredAt)
    {
        if (occurredAt > registeredAt)
            throw EconomicEventErrors.FuturePaidDate(occurredAt, registeredAt);

        var participations = new List<Participation>
        {
            new(payerAgentId, ParticipationRole.Provider),
            new(payeeAgentId, ParticipationRole.Recipient),
        };

        return RegisterCovered(
            id, tenantId, FlowDirection.Outflow, cashResourceId,
            new Money(amountValue, currency), new EventTimestamp(occurredAt), typeId: null,
            participations, new CommitmentRef(coveringContractId, coveringCommitmentId),
            new CompetencePeriod(competenceYear, competenceMonth), createdBy, registeredAt);
    }

    /// <summary>
    /// Factory for a single cash Outflow (one boleto) that settles SEVERAL covering commitments at once — the
    /// bundled-payment case (rent + condominium + property tax on one payment). The event's total Amount is the sum
    /// of the per-commitment <paramref name="lines"/>; each line becomes one allocation and its duality closes
    /// independently when the reciprocal consumption arrives. Composes all Value Objects internally so callers never
    /// assemble domain types. Rejects an empty line set (EVT17) and a paid date in the future (EVT15).
    /// </summary>
    public static EconomicEvent RegisterBundledPayment(
        EconomicEventId id,
        TenantId tenantId,
        EconomicResourceId cashResourceId,
        Currency currency,
        DateTime occurredAt,
        EconomicAgentId payerAgentId,
        EconomicAgentId payeeAgentId,
        IReadOnlyList<BundledPaymentLine> lines,
        int competenceYear,
        int competenceMonth,
        UserId? createdBy,
        DateTime registeredAt)
    {
        if (occurredAt > registeredAt)
            throw EconomicEventErrors.FuturePaidDate(occurredAt, registeredAt);
        if (lines is null || lines.Count == 0)
            throw EconomicEventErrors.EmptyAllocations();

        var participations = new List<Participation>
        {
            new(payerAgentId, ParticipationRole.Provider),
            new(payeeAgentId, ParticipationRole.Recipient),
        };

        var total = lines.Sum(l => l.Amount);
        var ev = BuildBase(
            id, tenantId, FlowDirection.Outflow, cashResourceId,
            new Money(total, currency), new EventTimestamp(occurredAt), typeId: null,
            participations, new CompetencePeriod(competenceYear, competenceMonth), createdBy, registeredAt);

        foreach (var line in lines)
            ev._allocations.Add(new PaymentAllocation(
                new CommitmentRef(EconomicContractId.From(line.ContractId), CommitmentId.From(line.CommitmentId)),
                new Money(line.Amount, currency)));

        ev.AddDomainEvent(new EconomicEventRegistered(
            EventId: Guid.NewGuid(),
            EconomicEventId: ev.Id,
            TenantId: ev.TenantId,
            DirectionName: ev.Direction.Name,
            ResourceId: ev.ResourceId,
            AmountValue: ev.Amount.Amount,
            AmountCurrency: ev.Amount.Currency.Name,
            CompetenceYear: ev.Competence.Year,
            CompetenceMonth: ev.Competence.Month,
            Coverings: [.. ev._allocations.Select(a => new EconomicEventCovering(a.Commitment.ContractId.Value, a.Commitment.CommitmentId.Value))],
            CounterpartEventId: null,
            OccurredAt: occurredAt));

        return ev;
    }

    /// <summary>
    /// Factory for an occupancy/consumption (Inflow) that covers an inflow commitment. Composes the event's Value
    /// Objects internally so callers never assemble domain types nor share VO instances with the commitment.
    /// </summary>
    public static EconomicEvent RegisterConsumptionCoverage(
        EconomicEventId id,
        TenantId tenantId,
        EconomicResourceId serviceResourceId,
        decimal amountValue,
        Currency currency,
        DateTime occurredAt,
        EconomicAgentId providerAgentId,
        EconomicAgentId recipientAgentId,
        EconomicContractId coveringContractId,
        CommitmentId coveringCommitmentId,
        int competenceYear,
        int competenceMonth,
        UserId? createdBy,
        DateTime registeredAt)
    {
        var participations = new List<Participation>
        {
            new(providerAgentId, ParticipationRole.Provider),
            new(recipientAgentId, ParticipationRole.Recipient),
        };

        return RegisterCovered(
            id, tenantId, FlowDirection.Inflow, serviceResourceId,
            new Money(amountValue, currency), new EventTimestamp(occurredAt), typeId: null,
            participations, new CommitmentRef(coveringContractId, coveringCommitmentId),
            new CompetencePeriod(competenceYear, competenceMonth), createdBy, registeredAt);
    }

    /// <summary>
    /// Registers an event already paired with its counterpart (simultaneous, or the 2nd to arrive in synchronous flow).
    /// The DualityLink is set at creation; the counterpart event must be created similarly in the same transaction.
    /// </summary>
    public static EconomicEvent RegisterPaired(
        EconomicEventId id,
        TenantId tenantId,
        FlowDirection direction,
        EconomicResourceId resourceId,
        Money amount,
        EventTimestamp occurredAt,
        EconomicEventTypeId? typeId,
        IReadOnlyCollection<Participation> participations,
        DualityLink duality,
        CompetencePeriod competence,
        UserId? createdBy,
        DateTime registeredAt)
    {
        if (duality is null)
            throw EconomicEventErrors.OrphanEvent();

        var ev = BuildBase(id, tenantId, direction, resourceId, amount, occurredAt, typeId, participations, competence, createdBy, registeredAt);
        ev._dualityLinks.Add(duality);

        ev.AddDomainEvent(new EconomicEventRegistered(
            EventId: Guid.NewGuid(),
            EconomicEventId: ev.Id,
            TenantId: ev.TenantId,
            DirectionName: ev.Direction.Name,
            ResourceId: ev.ResourceId,
            AmountValue: ev.Amount.Amount,
            AmountCurrency: ev.Amount.Currency.Name,
            CompetenceYear: ev.Competence.Year,
            CompetenceMonth: ev.Competence.Month,
            Coverings: [],
            CounterpartEventId: duality.CounterpartEventId.Value,
            OccurredAt: occurredAt.InstantUtc));

        return ev;
    }

    /// <summary>
    /// Closes (or partially closes) the duality leg for the allocation covering <paramref name="commitmentId"/>
    /// when its reciprocal event arrives. A bundled payment closes one leg per allocation; partial closes accumulate
    /// MatchedAmount per commitment. EVT18 if no allocation covers the commitment; EVT05 if that leg is already full.
    /// </summary>
    public void CloseDuality(CommitmentId commitmentId, EconomicEventId counterpartEventId, Money matchedAmount, DateTime occurredAt)
    {
        var allocation = _allocations.FirstOrDefault(a => a.Commitment.CommitmentId.Equals(commitmentId))
            ?? throw EconomicEventErrors.AllocationNotFound(commitmentId.Value);

        var currentMatched = MatchedAmountFor(commitmentId);
        if (currentMatched >= allocation.Amount.Amount)
            throw EconomicEventErrors.DualityAlreadyClosed();

        var remaining = allocation.Amount.Amount - currentMatched;
        if (matchedAmount is null || matchedAmount.Amount <= 0m || matchedAmount.Amount > remaining)
            throw EconomicEventErrors.MatchExceedsBalance(matchedAmount?.Amount ?? 0m, remaining);

        _dualityLinks.Add(new DualityLink(counterpartEventId, matchedAmount, commitmentId));
        UpdatedAt = occurredAt;

        AddDomainEvent(new DualityClosed(
            EventId: Guid.NewGuid(),
            EconomicEventId: Id,
            CounterpartEventId: counterpartEventId,
            MatchedAmountValue: matchedAmount.Amount,
            MatchedAmountCurrency: matchedAmount.Currency.Name,
            OccurredAt: occurredAt));
    }

    private static EconomicEvent BuildBase(
        EconomicEventId id,
        TenantId tenantId,
        FlowDirection direction,
        EconomicResourceId resourceId,
        Money amount,
        EventTimestamp occurredAt,
        EconomicEventTypeId? typeId,
        IReadOnlyCollection<Participation> participations,
        CompetencePeriod competence,
        UserId? createdBy,
        DateTime registeredAt)
    {
        var ev = new EconomicEvent(id)
        {
            TenantId = tenantId,
            Direction = direction,
            TypeId = typeId,
            Competence = competence,
            CreatedBy = createdBy,
            CreatedAt = registeredAt,
            UpdatedAt = registeredAt,
        };
        ev.SetResourceId(resourceId);
        ev.SetAmount(amount);
        ev.SetOccurredAt(occurredAt);
        ev.SetParticipations(participations);
        return ev;
    }

    private void SetResourceId(EconomicResourceId resourceId)
    {
        if (resourceId.Equals(EconomicResourceId.Empty))
            throw EconomicEventErrors.MissingResource();
        ResourceId = resourceId;
    }

    private void SetAmount(Money amount)
    {
        if (amount is null || amount.Amount <= 0m)
            throw EconomicEventErrors.InvalidAmount();
        Amount = amount;
    }

    private void SetOccurredAt(EventTimestamp occurredAt)
    {
        OccurredAt = occurredAt;
    }

    private void SetParticipations(IReadOnlyCollection<Participation> participations)
    {
        if (participations is null || participations.Count == 0)
            throw EconomicEventErrors.MissingParticipants();

        var hasProvider = participations.Any(p => p.Role == ParticipationRole.Provider);
        var hasRecipient = participations.Any(p => p.Role == ParticipationRole.Recipient);
        if (!hasProvider || !hasRecipient)
            throw EconomicEventErrors.MissingParticipants();

        _participations.Clear();
        _participations.AddRange(participations);
    }
}
