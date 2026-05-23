namespace EconomicCore.Domain.Operational.EconomicAgents.Enumerations;

using EconomicCore.Domain.SeedWork;

public sealed class AgentScope : Enumeration
{
    public static readonly AgentScope Inside = new(1, "INSIDE");
    public static readonly AgentScope Outside = new(2, "OUTSIDE");

    private AgentScope(int id, string name) : base(id, name) { }
}
