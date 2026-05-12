namespace AccountsPayable.Domain.ChartOfAccounts.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

public sealed class AccountName : ValueObject
{
    public const int MIN_LENGTH = 2;
    public const int MAX_LENGTH = 200;

    public string Value { get; }

    public AccountName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw AccountNameErrors.Empty();

        var trimmed = value.Trim();
        if (trimmed.Length < MIN_LENGTH)
            throw AccountNameErrors.TooShort(MIN_LENGTH);
        if (trimmed.Length > MAX_LENGTH)
            throw AccountNameErrors.TooLong(MAX_LENGTH);

        Value = trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
