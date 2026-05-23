namespace EconomicCore.Domain.Operational.EconomicResources.Enumerations;

using EconomicCore.Domain.SeedWork;

public sealed class ResourceKind : Enumeration
{
    public static readonly ResourceKind Cash = new(1, "CASH");
    public static readonly ResourceKind Service = new(2, "SERVICE");
    public static readonly ResourceKind LaborService = new(3, "LABOR_SERVICE");
    public static readonly ResourceKind FiscalObligation = new(4, "FISCAL_OBLIGATION");

    private ResourceKind(int id, string name) : base(id, name) { }
}
