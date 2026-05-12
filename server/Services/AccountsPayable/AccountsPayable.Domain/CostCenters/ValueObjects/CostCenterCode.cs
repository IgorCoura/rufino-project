namespace AccountsPayable.Domain.CostCenters.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Cost-center identifier: alphanumeric plus <c>-</c> and <c>_</c>, normalized to upper-case
/// (e.g., <c>OBRA-SYRAH</c>, <c>ESCRITORIO</c>, <c>FILIAL_01</c>).
/// </summary>
public sealed class CostCenterCode : ValueObject
{
    public const int MIN_LENGTH = 2;
    public const int MAX_LENGTH = 50;

    public string Value { get; }

    public CostCenterCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw CostCenterCodeErrors.Empty();

        var trimmed = value.Trim().ToUpperInvariant();
        if (trimmed.Length < MIN_LENGTH)
            throw CostCenterCodeErrors.TooShort(MIN_LENGTH);
        if (trimmed.Length > MAX_LENGTH)
            throw CostCenterCodeErrors.TooLong(MAX_LENGTH);
        if (!IsValidFormat(trimmed))
            throw CostCenterCodeErrors.InvalidFormat(trimmed);

        Value = trimmed;
    }

    private static bool IsValidFormat(string code)
    {
        foreach (var c in code)
        {
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
                return false;
        }
        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
