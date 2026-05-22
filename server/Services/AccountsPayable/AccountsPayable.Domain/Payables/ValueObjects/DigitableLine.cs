namespace AccountsPayable.Domain.Payables.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Linha digitável do boleto bancário (47 dígitos). Forma alfanumérica humanamente legível do
/// código de barras, normalmente apresentada em 5 grupos separados por espaços ou pontos.
/// O VO armazena apenas os dígitos. Boletos de concessionária (48 dígitos) não são suportados
/// no MVP — quando entrar, será uma variante separada.
/// </summary>
public sealed class DigitableLine : ValueObject
{
    public const int LENGTH = 47;

    public string Value { get; }

    public DigitableLine(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw DigitableLineErrors.Empty();

        var trimmed = value.Trim();
        if (!trimmed.All(c => char.IsDigit(c) || char.IsWhiteSpace(c) || c == '.' || c == '-'))
            throw DigitableLineErrors.NonNumeric();

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length != LENGTH)
            throw DigitableLineErrors.InvalidLength(digits.Length, LENGTH);

        Value = digits;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
