namespace EconomicCore.Domain.Operational.EconomicResources;

using EconomicCore.Domain.SeedWork;

public readonly record struct EconomicResourceTypeId(Guid Value) : IEntityId<EconomicResourceTypeId>
{
    public static EconomicResourceTypeId New() => new(Guid.CreateVersion7());
    public static EconomicResourceTypeId From(Guid value) => new(value);
    public static EconomicResourceTypeId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
