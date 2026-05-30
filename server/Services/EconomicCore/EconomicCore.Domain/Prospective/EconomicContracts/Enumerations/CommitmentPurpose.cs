namespace EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;

using EconomicCore.Domain.SeedWork;

/// <summary>
/// Distinguishes the economic phenomenon a commitment track represents inside a lease contract.
/// Rent is the contract core; Condominium/Insurance/PropertyTax are additional charge tracks; Penalty
/// is born on breach (late payment). Pragmatic stand-in for the REA Type Pattern (Hruby §5.12): lets the
/// contract generate per-track reciprocal pairs and the DRE group expenses by charge ("por que paguei isso?").
/// </summary>
public sealed class CommitmentPurpose : Enumeration
{
    public static readonly CommitmentPurpose Rent = new(1, "RENT");
    public static readonly CommitmentPurpose Condominium = new(2, "CONDOMINIUM");
    public static readonly CommitmentPurpose Insurance = new(3, "INSURANCE");
    public static readonly CommitmentPurpose PropertyTax = new(4, "PROPERTY_TAX");
    public static readonly CommitmentPurpose Penalty = new(5, "PENALTY");

    private CommitmentPurpose(int id, string name) : base(id, name) { }
}
