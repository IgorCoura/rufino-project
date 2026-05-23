namespace EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed class CommitmentTerms : ValueObject
{
    public const decimal MIN_TOLERANCE_PERCENT = 0m;
    public const decimal MAX_TOLERANCE_PERCENT = 1m;
    public const int MIN_WINDOW_DAYS = 0;

    public Money ExpectedAmount { get; }
    public decimal TolerancePercent { get; }
    public int WindowDays { get; }

    public CommitmentTerms(Money expectedAmount, decimal tolerancePercent, int windowDays)
    {
        if (expectedAmount is null || expectedAmount.Amount <= 0m)
            throw EconomicContractErrors.InvalidCommitmentTermsAmount();
        if (tolerancePercent < MIN_TOLERANCE_PERCENT || tolerancePercent > MAX_TOLERANCE_PERCENT)
            throw EconomicContractErrors.InvalidCommitmentTermsTolerance(tolerancePercent);
        if (windowDays < MIN_WINDOW_DAYS)
            throw EconomicContractErrors.InvalidCommitmentTermsWindow(windowDays);

        ExpectedAmount = expectedAmount;
        TolerancePercent = tolerancePercent;
        WindowDays = windowDays;
    }

    public bool IsWithinTolerance(Money actual)
    {
        if (actual is null) return false;
        if (!actual.Currency.Equals(ExpectedAmount.Currency)) return false;
        var diff = Math.Abs(actual.Amount - ExpectedAmount.Amount);
        var allowed = ExpectedAmount.Amount * TolerancePercent;
        return diff <= allowed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ExpectedAmount;
        yield return TolerancePercent;
        yield return WindowDays;
    }
}
