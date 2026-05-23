namespace EconomicCore.Domain.SharedKernel;

using EconomicCore.Domain.SeedWork;

public sealed class TaxId : ValueObject
{
    private static readonly int[] CPF_WEIGHTS_FIRST = [10, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] CPF_WEIGHTS_SECOND = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] CNPJ_WEIGHTS_FIRST = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] CNPJ_WEIGHTS_SECOND = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    public string Value { get; }
    public TaxIdKind Kind { get; }

    public TaxId(string value, TaxIdKind kind)
    {
        if (kind is null)
            throw TaxIdErrors.KindRequired();
        if (string.IsNullOrWhiteSpace(value))
            throw TaxIdErrors.InvalidFormat(value ?? string.Empty);

        var sanitized = new string(value.Where(char.IsDigit).ToArray());
        if (sanitized.Length != kind.ExpectedLength)
            throw TaxIdErrors.InvalidFormat(value);
        if (!HasValidCheckDigits(sanitized, kind))
            throw TaxIdErrors.InvalidCheckDigit(value);

        Value = sanitized;
        Kind = kind;
    }

    public string Formatted()
    {
        if (Kind == TaxIdKind.CPF)
            return $"{Value[..3]}.{Value[3..6]}.{Value[6..9]}-{Value[9..]}";
        return $"{Value[..2]}.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-{Value[12..]}";
    }

    private static bool HasValidCheckDigits(string digits, TaxIdKind kind)
    {
        // Reject blacklist of repeated-digit values (e.g. 11111111111, 22222222222 — formal length match but invalid).
        if (digits.Distinct().Count() == 1)
            return false;

        if (kind == TaxIdKind.CPF)
            return ComputeMod11Digit(digits.AsSpan(0, 9), CPF_WEIGHTS_FIRST) == (digits[9] - '0')
                && ComputeMod11Digit(digits.AsSpan(0, 10), CPF_WEIGHTS_SECOND) == (digits[10] - '0');

        if (kind == TaxIdKind.CNPJ)
            return ComputeMod11Digit(digits.AsSpan(0, 12), CNPJ_WEIGHTS_FIRST) == (digits[12] - '0')
                && ComputeMod11Digit(digits.AsSpan(0, 13), CNPJ_WEIGHTS_SECOND) == (digits[13] - '0');

        return false;
    }

    private static int ComputeMod11Digit(ReadOnlySpan<char> digits, int[] weights)
    {
        var sum = 0;
        for (int i = 0; i < digits.Length; i++)
            sum += (digits[i] - '0') * weights[i];
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
        yield return Kind;
    }
}
