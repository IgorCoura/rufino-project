namespace AccountsPayable.Domain.Suppliers.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Brazilian phone number normalized to digits only: 10 dígitos (fixo: DDD + 8) ou
/// 11 dígitos (celular: DDD + 9).
/// </summary>
public sealed class PhoneNumber : ValueObject
{
    public const int MIN_LENGTH = 10;
    public const int MAX_LENGTH = 11;

    public string Value { get; }

    public PhoneNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw PhoneNumberErrors.Empty();

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length < MIN_LENGTH || digits.Length > MAX_LENGTH)
            throw PhoneNumberErrors.InvalidLength(MIN_LENGTH, MAX_LENGTH);

        Value = digits;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
