namespace AccountsPayable.Domain.CostCenters.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

public sealed class CostCenterName : ValueObject
{
    public const int MIN_LENGTH = 2;
    public const int MAX_LENGTH = 200;

    public string Value { get; }

    public CostCenterName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw CostCenterNameErrors.Empty();

        var trimmed = value.Trim();
        if (trimmed.Length < MIN_LENGTH)
            throw CostCenterNameErrors.TooShort(MIN_LENGTH);
        if (trimmed.Length > MAX_LENGTH)
            throw CostCenterNameErrors.TooLong(MAX_LENGTH);

        Value = trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
