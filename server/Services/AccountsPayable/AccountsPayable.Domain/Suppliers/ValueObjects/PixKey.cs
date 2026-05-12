namespace AccountsPayable.Domain.Suppliers.ValueObjects;

using System.Net.Mail;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Enumerations;

/// <summary>
/// PIX key — Brazilian instant-payment identifier. The shape of <see cref="Value"/> depends
/// on <see cref="Type"/>: CPF (11 digits), CNPJ (14 digits), Email, Phone (E.164-ish, BR format),
/// or Random (UUID-like, 32 hex chars).
/// </summary>
public sealed class PixKey : ValueObject
{
    public const int RANDOM_KEY_LENGTH = 32;

    public string Value { get; }
    public PixKeyType Type { get; }

    public PixKey(string value, PixKeyType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (string.IsNullOrWhiteSpace(value))
            throw PixKeyErrors.Empty();

        var raw = value.Trim();
        Value = Normalize(raw, type);
        Type = type;
    }

    private static string Normalize(string raw, PixKeyType type)
    {
        if (type == PixKeyType.Cpf)
        {
            var digits = OnlyDigits(raw);
            if (digits.Length != TaxId.CPF_LENGTH)
                throw PixKeyErrors.InvalidForType(type.Name);
            return digits;
        }

        if (type == PixKeyType.Cnpj)
        {
            var digits = OnlyDigits(raw);
            if (digits.Length != TaxId.CNPJ_LENGTH)
                throw PixKeyErrors.InvalidForType(type.Name);
            return digits;
        }

        if (type == PixKeyType.Email)
        {
            if (!MailAddress.TryCreate(raw, out _))
                throw PixKeyErrors.InvalidForType(type.Name);
            return raw.ToLowerInvariant();
        }

        if (type == PixKeyType.Phone)
        {
            var digits = OnlyDigits(raw);
            if (digits.Length < PhoneNumber.MIN_LENGTH || digits.Length > PhoneNumber.MAX_LENGTH)
                throw PixKeyErrors.InvalidForType(type.Name);
            return digits;
        }

        if (type == PixKeyType.Random)
        {
            var compact = new string(raw.Where(c => char.IsLetterOrDigit(c)).ToArray());
            if (compact.Length != RANDOM_KEY_LENGTH || !compact.All(IsHexChar))
                throw PixKeyErrors.InvalidForType(type.Name);
            return compact.ToLowerInvariant();
        }

        throw PixKeyErrors.InvalidForType(type.Name);
    }

    private static string OnlyDigits(string raw) => new(raw.Where(char.IsDigit).ToArray());
    private static bool IsHexChar(char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Type;
    }
}
