namespace EconomicCore.Domain.Operational.EconomicEvents;

using EconomicCore.Domain.SeedWork;

public readonly record struct EconomicEventId(Guid Value) : IEntityId<EconomicEventId>
{
    public static EconomicEventId New() => new(Guid.CreateVersion7());
    public static EconomicEventId From(Guid value) => new(value);
    public static EconomicEventId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
