namespace AccountsPayable.Domain.Suppliers.ValueObjects;

using System.Net.Mail;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

public sealed class EmailAddress : ValueObject
{
    public const int MAX_LENGTH = 254; // RFC 5321

    public string Value { get; }

    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw EmailAddressErrors.Empty();

        var trimmed = value.Trim();
        if (trimmed.Length > MAX_LENGTH)
            throw EmailAddressErrors.TooLong(MAX_LENGTH);

        if (!MailAddress.TryCreate(trimmed, out _))
            throw EmailAddressErrors.Invalid(trimmed);

        Value = trimmed.ToLowerInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
