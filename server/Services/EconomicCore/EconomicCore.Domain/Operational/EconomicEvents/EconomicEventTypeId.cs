namespace EconomicCore.Domain.Operational.EconomicEvents;

using EconomicCore.Domain.SeedWork;

public readonly record struct EconomicEventTypeId(Guid Value) : IEntityId<EconomicEventTypeId>
{
    public static EconomicEventTypeId New() => new(Guid.CreateVersion7());
    public static EconomicEventTypeId From(Guid value) => new(value);
    public static EconomicEventTypeId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
