namespace EconomicCore.Domain.Operational.EconomicAgents;

using EconomicCore.Domain.SeedWork;

public readonly record struct EconomicRoleId(Guid Value) : IEntityId<EconomicRoleId>
{
    public static EconomicRoleId New() => new(Guid.CreateVersion7());
    public static EconomicRoleId From(Guid value) => new(value);
    public static EconomicRoleId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
