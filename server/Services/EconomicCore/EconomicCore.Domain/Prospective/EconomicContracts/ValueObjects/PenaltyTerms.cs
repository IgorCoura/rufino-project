namespace EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

/// <summary>
/// Late-payment penalty policy of a contract: a one-off fine (multa) plus monthly interest (juros de mora).
/// When a commitment is paid after its fulfillment window, the contract materializes a Penalty commitment whose
/// amount is <c>base × (FinePercent + MonthlyInterestPercent × monthsLate)</c> — the Penalty pattern (Hruby §10.5):
/// the obligation is born on breach, not pre-generated.
/// </summary>
public sealed class PenaltyTerms : ValueObject
{
    public const decimal MIN_PERCENT = 0m;
    public const decimal MAX_PERCENT = 1m;

    public decimal FinePercent { get; private set; }
    public decimal MonthlyInterestPercent { get; private set; }

    private PenaltyTerms() { }

    public PenaltyTerms(decimal finePercent, decimal monthlyInterestPercent)
    {
        if (finePercent < MIN_PERCENT || finePercent > MAX_PERCENT)
            throw EconomicContractErrors.InvalidPenaltyTerms(finePercent);
        if (monthlyInterestPercent < MIN_PERCENT || monthlyInterestPercent > MAX_PERCENT)
            throw EconomicContractErrors.InvalidPenaltyTerms(monthlyInterestPercent);

        FinePercent = finePercent;
        MonthlyInterestPercent = monthlyInterestPercent;
    }

    /// <summary>Penalty owed on <paramref name="baseAmount"/> paid <paramref name="monthsLate"/> full months late.</summary>
    public Money ComputePenalty(Money baseAmount, int monthsLate)
    {
        var months = monthsLate < 0 ? 0 : monthsLate;
        var factor = FinePercent + (MonthlyInterestPercent * months);
        return baseAmount.Multiply(factor);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FinePercent;
        yield return MonthlyInterestPercent;
    }
}
