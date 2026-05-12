namespace AccountsPayable.Domain.ChartOfAccounts.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

public sealed class ChartOfAccountsName : ValueObject
{
    public const int MIN_LENGTH = 2;
    public const int MAX_LENGTH = 200;

    public string Value { get; }

    public ChartOfAccountsName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw ChartOfAccountsNameErrors.Empty();

        var trimmed = value.Trim();
        if (trimmed.Length < MIN_LENGTH)
            throw ChartOfAccountsNameErrors.TooShort(MIN_LENGTH);
        if (trimmed.Length > MAX_LENGTH)
            throw ChartOfAccountsNameErrors.TooLong(MAX_LENGTH);

        Value = trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
