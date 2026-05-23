namespace EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;

using EconomicCore.Domain.SeedWork;

public sealed class CommitmentDirection : Enumeration
{
    public static readonly CommitmentDirection OutflowPromise = new(1, "OUTFLOW_PROMISE");
    public static readonly CommitmentDirection InflowPromise = new(2, "INFLOW_PROMISE");

    private CommitmentDirection(int id, string name) : base(id, name) { }
}
