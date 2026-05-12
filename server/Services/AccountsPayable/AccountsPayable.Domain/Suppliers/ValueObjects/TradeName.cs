namespace AccountsPayable.Domain.Suppliers.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

public sealed class TradeName : ValueObject
{
    public const int MAX_LENGTH = 200;

    public string Value { get; }

    public TradeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw TradeNameErrors.Empty();

        var trimmed = value.Trim();
        if (trimmed.Length > MAX_LENGTH)
            throw TradeNameErrors.TooLong(MAX_LENGTH);

        Value = trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
