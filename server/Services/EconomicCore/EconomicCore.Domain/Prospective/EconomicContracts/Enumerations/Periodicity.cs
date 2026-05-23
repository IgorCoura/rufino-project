namespace EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;

using EconomicCore.Domain.SeedWork;

public sealed class Periodicity : Enumeration
{
    public static readonly Periodicity Monthly = new(1, "MONTHLY");
    public static readonly Periodicity Weekly = new(2, "WEEKLY");
    public static readonly Periodicity Yearly = new(3, "YEARLY");

    private Periodicity(int id, string name) : base(id, name) { }
}
