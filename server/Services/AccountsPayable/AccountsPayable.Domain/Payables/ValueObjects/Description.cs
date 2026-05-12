namespace AccountsPayable.Domain.Payables.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

public sealed class Description : ValueObject
{
    public const int MAX_LENGTH = 500;

    public string Value { get; }

    public Description(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw DescriptionErrors.Empty();

        var trimmed = value.Trim();
        if (trimmed.Length > MAX_LENGTH)
            throw DescriptionErrors.TooLong(MAX_LENGTH);

        Value = trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
