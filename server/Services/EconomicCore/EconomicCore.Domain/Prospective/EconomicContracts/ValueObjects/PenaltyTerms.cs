namespace EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;

using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

/// <summary>
/// Late-payment penalty policy of a contract: a one-off fine (multa) plus interest (juros de mora) accruing
/// per fully elapsed <see cref="InterestAccrualPeriod"/> unit. Fine and interest are each expressed either as
/// a percentage of the commitment's expected amount or as a fixed amount in the commitment's currency
/// (<see cref="PenaltyValueKind"/>). When a commitment is paid after its fulfillment window, the contract
/// materializes a Penalty commitment priced by <see cref="ComputePenalty"/> — the Penalty pattern (Hruby §10.5):
/// the obligation is born on breach, not pre-generated.
/// </summary>
public sealed class PenaltyTerms : ValueObject
{
    public const decimal MIN_PERCENT = 0m;
    public const decimal MAX_PERCENT = 1m;

    public PenaltyValueKind FineKind { get; private set; } = default!;
    public decimal FineValue { get; private set; }
    public PenaltyValueKind InterestKind { get; private set; } = default!;
    public decimal InterestValue { get; private set; }
    public InterestAccrualPeriod InterestPeriod { get; private set; } = default!;

    private PenaltyTerms() { }

    public PenaltyTerms(
        PenaltyValueKind fineKind,
        decimal fineValue,
        PenaltyValueKind interestKind,
        decimal interestValue,
        InterestAccrualPeriod interestPeriod)
    {
        if (fineKind is null || interestKind is null || interestPeriod is null)
            throw EconomicContractErrors.InvalidPenaltyTermsComposition();

        FineKind = fineKind;
        FineValue = ValidateComponent(fineKind, fineValue);
        InterestKind = interestKind;
        InterestValue = ValidateComponent(interestKind, interestValue);
        InterestPeriod = interestPeriod;
    }

    /// <summary>
    /// Penalty owed on <paramref name="baseAmount"/> settled at <paramref name="paidDate"/> for a commitment
    /// due at <paramref name="dueDate"/>: one-off fine + interest × fully elapsed accrual units. Fixed
    /// components are denominated in the base amount's currency (contracts are single-currency).
    /// </summary>
    public Money ComputePenalty(Money baseAmount, DateOnly dueDate, DateOnly paidDate)
    {
        var units = InterestPeriod.ElapsedUnits(dueDate, paidDate);

        var fine = FineKind == PenaltyValueKind.Percent
            ? baseAmount.Multiply(FineValue)
            : new Money(FineValue, baseAmount.Currency);

        var interest = InterestKind == PenaltyValueKind.Percent
            ? baseAmount.Multiply(InterestValue * units)
            : new Money(InterestValue * units, baseAmount.Currency);

        return fine.Add(interest);
    }

    private static decimal ValidateComponent(PenaltyValueKind kind, decimal value)
    {
        if (kind == PenaltyValueKind.Percent && (value < MIN_PERCENT || value > MAX_PERCENT))
            throw EconomicContractErrors.InvalidPenaltyTerms(value);
        if (kind == PenaltyValueKind.FixedAmount && value < 0m)
            throw EconomicContractErrors.InvalidPenaltyFixedValue(value);

        return value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FineKind;
        yield return FineValue;
        yield return InterestKind;
        yield return InterestValue;
        yield return InterestPeriod;
    }
}
