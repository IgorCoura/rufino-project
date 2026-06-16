namespace EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;

using EconomicCore.Domain.SeedWork;

/// <summary>
/// How a penalty component (fine or interest) is expressed: a percentage of the commitment's
/// expected amount, or a fixed monetary amount in the commitment's currency.
/// </summary>
public sealed class PenaltyValueKind : Enumeration
{
    public static readonly PenaltyValueKind Percent = new(1, "PERCENT");
    public static readonly PenaltyValueKind FixedAmount = new(2, "FIXED");

    private PenaltyValueKind(int id, string name) : base(id, name) { }
}
