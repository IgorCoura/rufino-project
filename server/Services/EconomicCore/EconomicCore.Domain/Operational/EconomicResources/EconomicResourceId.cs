namespace EconomicCore.Domain.Operational.EconomicResources;

using EconomicCore.Domain.SeedWork;

public readonly record struct EconomicResourceId(Guid Value) : IEntityId<EconomicResourceId>
{
    public static EconomicResourceId New() => new(Guid.CreateVersion7());
    public static EconomicResourceId From(Guid value) => new(value);
    public static EconomicResourceId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
