namespace EconomicCore.Domain.Operational.EconomicEvents;

using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.Operational.EconomicEvents.Events;
using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed class EconomicEvent : AggregateRoot<EconomicEventId>
{
    private readonly List<Participation> _participations = [];

    public TenantId TenantId { get; private set; }
    public FlowDirection Direction { get; private set; } = default!;
    public EconomicResourceId ResourceId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public EventTimestamp OccurredAt { get; private set; } = default!;
    public EconomicEventTypeId? TypeId { get; private set; }
    public IReadOnlyCollection<Participation> Participations => _participations.AsReadOnly();
    public DualityLink? Duality { get; private set; }
    public CommitmentRef? CoveringCommitment { get; private set; }
    public CompetencePeriod Competence { get; private set; } = default!;
    public UserId? CreatedBy { get; private set; }

    private EconomicEvent() : base() { }
    private EconomicEvent(EconomicEventId id) : base(id) { }

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
        ev.CoveringCommitment = coveringCommitment;

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
            CoveringCommitmentId: coveringCommitment.CommitmentId.Value,
            CounterpartEventId: null,
            OccurredAt: occurredAt.InstantUtc));

        return ev;
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
        ev.Duality = duality;

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
            CoveringCommitmentId: null,
            CounterpartEventId: duality.CounterpartEventId.Value,
            OccurredAt: occurredAt.InstantUtc));

        return ev;
    }

    /// <summary>
    /// Closes (or partially closes) the duality when the reciprocal event arrives.
    /// Phase 1 simplification: a single counterpart per event; partial closes accumulate MatchedAmount.
    /// </summary>
    public void CloseDuality(EconomicEventId counterpartEventId, Money matchedAmount, DateTime occurredAt)
    {
        var currentMatched = Duality?.MatchedAmount.Amount ?? 0m;
        if (currentMatched >= Amount.Amount)
            throw EconomicEventErrors.DualityAlreadyClosed();

        var remaining = Amount.Amount - currentMatched;
        if (matchedAmount is null || matchedAmount.Amount <= 0m || matchedAmount.Amount > remaining)
            throw EconomicEventErrors.MatchExceedsBalance(matchedAmount?.Amount ?? 0m, remaining);

        var accumulated = new Money(currentMatched + matchedAmount.Amount, Amount.Currency);
        Duality = new DualityLink(counterpartEventId, accumulated);
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
