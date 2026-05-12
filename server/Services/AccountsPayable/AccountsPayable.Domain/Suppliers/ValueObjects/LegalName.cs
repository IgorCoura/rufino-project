namespace AccountsPayable.Domain.Suppliers.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

public sealed class LegalName : ValueObject
{
    public const int MIN_LENGTH = 2;
    public const int MAX_LENGTH = 200;

    public string Value { get; }

    public LegalName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw LegalNameErrors.Empty();

        var trimmed = value.Trim();
        if (trimmed.Length < MIN_LENGTH)
            throw LegalNameErrors.TooShort(MIN_LENGTH);
        if (trimmed.Length > MAX_LENGTH)
            throw LegalNameErrors.TooLong(MAX_LENGTH);

        Value = trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
