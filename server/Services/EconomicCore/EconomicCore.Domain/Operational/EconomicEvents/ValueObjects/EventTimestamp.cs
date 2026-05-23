namespace EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;

using EconomicCore.Domain.SeedWork;

public sealed class EventTimestamp : ValueObject
{
    public DateTime InstantUtc { get; }

    public EventTimestamp(DateTime instantUtc)
    {
        if (instantUtc.Kind != DateTimeKind.Utc)
            throw EconomicEventErrors.InvalidEventTimestamp(instantUtc.Kind.ToString());

        InstantUtc = instantUtc;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return InstantUtc;
    }
}
