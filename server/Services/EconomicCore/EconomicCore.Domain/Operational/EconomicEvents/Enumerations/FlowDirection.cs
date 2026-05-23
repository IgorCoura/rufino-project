namespace EconomicCore.Domain.Operational.EconomicEvents.Enumerations;

using EconomicCore.Domain.SeedWork;

public sealed class FlowDirection : Enumeration
{
    public static readonly FlowDirection Inflow = new(1, "INFLOW");
    public static readonly FlowDirection Outflow = new(2, "OUTFLOW");

    private FlowDirection(int id, string name) : base(id, name) { }
}
