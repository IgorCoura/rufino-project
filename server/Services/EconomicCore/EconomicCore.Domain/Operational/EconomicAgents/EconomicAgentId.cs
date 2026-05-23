namespace EconomicCore.Domain.Operational.EconomicAgents;

using EconomicCore.Domain.SeedWork;

public readonly record struct EconomicAgentId(Guid Value) : IEntityId<EconomicAgentId>
{
    public static EconomicAgentId New() => new(Guid.CreateVersion7());
    public static EconomicAgentId From(Guid value) => new(value);
    public static EconomicAgentId Empty => new(Guid.Empty);
    public override string ToString() => Value.ToString();
}
