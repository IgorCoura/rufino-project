namespace AccountsPayable.Domain.Suppliers.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Enumerations;

/// <summary>
/// Brazilian tax identifier (CPF for individuals, 11 digits; CNPJ for companies, 14 digits).
/// Stores the normalized digits-only form; exposes <see cref="Formatted"/> for display and
/// <see cref="MaskedValue"/> for logs/errors (PII protection).
/// </summary>
public sealed class TaxId : ValueObject
{
    public const int CPF_LENGTH = 11;
    public const int CNPJ_LENGTH = 14;

    private static readonly int[] CNPJ_MULTIPLIERS_FIRST = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
    private static readonly int[] CNPJ_MULTIPLIERS_SECOND = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

    public string Value { get; }
    public TaxIdType Type { get; }

    public TaxId(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw TaxIdErrors.Empty();

        var digits = new string(raw.Where(char.IsDigit).ToArray());

        if (digits.Length == CPF_LENGTH && IsValidCpf(digits))
        {
            Value = digits;
            Type = TaxIdType.Cpf;
        }
        else if (digits.Length == CNPJ_LENGTH && IsValidCnpj(digits))
        {
            Value = digits;
            Type = TaxIdType.Cnpj;
        }
        else
        {
            throw TaxIdErrors.Invalid(raw);
        }
    }

    public string Formatted => Type == TaxIdType.Cpf
        ? $"{Value.Substring(0, 3)}.{Value.Substring(3, 3)}.{Value.Substring(6, 3)}-{Value.Substring(9, 2)}"
        : $"{Value.Substring(0, 2)}.{Value.Substring(2, 3)}.{Value.Substring(5, 3)}/{Value.Substring(8, 4)}-{Value.Substring(12, 2)}";

    public string MaskedValue => Type == TaxIdType.Cpf
        ? $"***.***.{Value.Substring(6, 3)}-{Value.Substring(9, 2)}"
        : $"**.***.***/{Value.Substring(8, 4)}-{Value.Substring(12, 2)}";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Type;
    }

    private static bool IsValidCpf(string digits)
    {
        if (digits.All(c => c == digits[0]))
            return false;

        int sum1 = 0;
        for (int i = 0; i < 9; i++)
            sum1 += (digits[i] - '0') * (10 - i);
        int d1 = sum1 % 11;
        d1 = d1 < 2 ? 0 : 11 - d1;
        if (d1 != digits[9] - '0') return false;

        int sum2 = 0;
        for (int i = 0; i < 10; i++)
            sum2 += (digits[i] - '0') * (11 - i);
        int d2 = sum2 % 11;
        d2 = d2 < 2 ? 0 : 11 - d2;
        return d2 == digits[10] - '0';
    }

    private static bool IsValidCnpj(string digits)
    {
        if (digits.All(c => c == digits[0]))
            return false;

        int sum1 = 0;
        for (int i = 0; i < 12; i++)
            sum1 += (digits[i] - '0') * CNPJ_MULTIPLIERS_FIRST[i];
        int d1 = sum1 % 11;
        d1 = d1 < 2 ? 0 : 11 - d1;
        if (d1 != digits[12] - '0') return false;

        int sum2 = 0;
        for (int i = 0; i < 13; i++)
            sum2 += (digits[i] - '0') * CNPJ_MULTIPLIERS_SECOND[i];
        int d2 = sum2 % 11;
        d2 = d2 < 2 ? 0 : 11 - d2;
        return d2 == digits[13] - '0';
    }
}
