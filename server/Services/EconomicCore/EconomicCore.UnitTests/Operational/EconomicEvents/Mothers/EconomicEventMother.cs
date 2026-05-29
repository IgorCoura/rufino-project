namespace EconomicCore.UnitTests.Operational.EconomicEvents.Mothers;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SharedKernel;

public sealed class EconomicEventMother
{
    public static readonly DateTime FixedRegisteredAt = new(2025, 10, 1, 10, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime FixedOccurredAtUtc = new(2025, 9, 30, 23, 59, 59, DateTimeKind.Utc);
    public static readonly TenantId FixedTenantId = TenantId.From(new Guid("11111111-1111-7111-8111-111111111111"));
    public static readonly EconomicEventId FixedEventId = EconomicEventId.From(new Guid("66666666-6666-7666-8666-666666666666"));
    public static readonly EconomicResourceId FixedResourceId = EconomicResourceId.From(new Guid("77777777-7777-7777-8777-777777777777"));
    public static readonly EconomicAgentId ProviderAgentId = EconomicAgentId.From(new Guid("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa"));
    public static readonly EconomicAgentId RecipientAgentId = EconomicAgentId.From(new Guid("bbbbbbbb-bbbb-7bbb-8bbb-bbbbbbbbbbbb"));
    public static readonly EconomicContractId FixedContractId = EconomicContractId.From(new Guid("eeeeeeee-eeee-7eee-8eee-eeeeeeeeeeee"));
    public static readonly CommitmentId FixedCommitmentId = CommitmentId.From(new Guid("cccccccc-cccc-7ccc-8ccc-cccccccccccc"));
    public static readonly EconomicEventId CounterpartEventId = EconomicEventId.From(new Guid("dddddddd-dddd-7ddd-8ddd-dddddddddddd"));

    public static Money DefaultAmount() => new(1000m, Currency.BRL);

    public static EventTimestamp DefaultOccurredAt() => new(FixedOccurredAtUtc);

    public static CompetencePeriod DefaultCompetence() => new(2025, 9);

    public static IReadOnlyCollection<Participation> DefaultParticipations() =>
    [
        new Participation(ProviderAgentId, ParticipationRole.Provider),
        new Participation(RecipientAgentId, ParticipationRole.Recipient),
    ];

    public static CommitmentRef DefaultCommitment() => new(FixedContractId, FixedCommitmentId);

    public static DualityLink DefaultDuality() => new(CounterpartEventId, DefaultAmount());

    private EconomicEventId _id = FixedEventId;
    private TenantId _tenantId = FixedTenantId;
    private FlowDirection _direction = FlowDirection.Outflow;
    private EconomicResourceId _resourceId = FixedResourceId;
    private Money _amount = DefaultAmount();
    private EventTimestamp _occurredAt = DefaultOccurredAt();
    private EconomicEventTypeId? _typeId;
    private IReadOnlyCollection<Participation> _participations = DefaultParticipations();
    private CompetencePeriod _competence = DefaultCompetence();
    private UserId? _createdBy;
    private DateTime _registeredAt = FixedRegisteredAt;

    public static EconomicEventMother New() => new();

    public EconomicEventMother WithId(EconomicEventId id)
    {
        _id = id;
        return this;
    }

    public EconomicEventMother WithDirection(FlowDirection direction)
    {
        _direction = direction;
        return this;
    }

    public EconomicEventMother WithResourceId(EconomicResourceId resourceId)
    {
        _resourceId = resourceId;
        return this;
    }

    public EconomicEventMother WithAmount(Money amount)
    {
        _amount = amount;
        return this;
    }

    public EconomicEventMother WithOccurredAt(EventTimestamp occurredAt)
    {
        _occurredAt = occurredAt;
        return this;
    }

    public EconomicEventMother WithParticipations(IReadOnlyCollection<Participation> participations)
    {
        _participations = participations;
        return this;
    }

    public EconomicEventMother WithCompetence(CompetencePeriod competence)
    {
        _competence = competence;
        return this;
    }

    public EconomicEventMother At(DateTime registeredAt)
    {
        _registeredAt = registeredAt;
        return this;
    }

    public EconomicEventMother WithTypeId(EconomicEventTypeId typeId)
    {
        _typeId = typeId;
        return this;
    }

    public EconomicEventMother WithCreatedBy(UserId createdBy)
    {
        _createdBy = createdBy;
        return this;
    }

    public EconomicEvent BuildCovered() => BuildCoveredWith(DefaultCommitment());

    public EconomicEvent BuildCoveredWith(CommitmentRef commitment)
        => EconomicEvent.RegisterCovered(
            _id, _tenantId, _direction, _resourceId, _amount, _occurredAt, _typeId,
            _participations, commitment, _competence, _createdBy, _registeredAt);

    public EconomicEvent BuildPaired() => BuildPairedWith(DefaultDuality());

    public EconomicEvent BuildPairedWith(DualityLink duality)
        => EconomicEvent.RegisterPaired(
            _id, _tenantId, _direction, _resourceId, _amount, _occurredAt, _typeId,
            _participations, duality, _competence, _createdBy, _registeredAt);
}
