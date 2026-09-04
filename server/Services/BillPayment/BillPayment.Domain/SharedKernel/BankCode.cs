namespace BillPayment.Domain.SharedKernel;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Código COMPE de três dígitos do banco (341 Itaú, 033 Santander, 237 Bradesco...).
/// Aceita entrada com menos de três dígitos e normaliza com zeros à esquerda — "33" e "033"
/// designam o mesmo banco, e divergir nisso quebraria o check de banco recebedor.
/// </summary>
public sealed class BankCode : ValueObject
{
    public const int LENGTH = 3;

    private const string UNASSIGNED = "000";

    public string Value { get; private set; } = string.Empty;

    private BankCode() { }

    public BankCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw BankCodeErrors.Required();

        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 0 || digits.Length > LENGTH)
            throw BankCodeErrors.InvalidFormat(value);

        var normalized = digits.PadLeft(LENGTH, '0');
        if (string.Equals(normalized, UNASSIGNED, StringComparison.Ordinal))
            throw BankCodeErrors.InvalidFormat(value);

        Value = normalized;
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
