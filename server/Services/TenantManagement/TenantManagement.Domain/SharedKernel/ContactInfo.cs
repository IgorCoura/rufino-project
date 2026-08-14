namespace TenantManagement.Domain.SharedKernel;

using TenantManagement.Domain.SeedWork;

/// <summary>
/// Como o sistema fala com o tenant. O e-mail é obrigatório porque é por ele que chega
/// o convite de acesso; o telefone é opcional.
/// </summary>
public sealed class ContactInfo : ValueObject
{
    public const int MAX_LENGTH_EMAIL = 200;
    public const int MIN_LENGTH_PHONE = 10;
    public const int MAX_LENGTH_PHONE = 11;

    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;

    private ContactInfo() { }

    public static ContactInfo Create(string email, string? phone = null)
        => new()
        {
            Email = NormalizeEmail(email),
            Phone = NormalizePhone(phone),
        };

    private static string NormalizeEmail(string email)
    {
        var normalized = EmailSyntax.Normalize(email);
        if (normalized.Length == 0)
            throw ContactInfoErrors.EmailRequired();
        if (normalized.Length > MAX_LENGTH_EMAIL || !EmailSyntax.IsValidAddress(normalized))
            throw ContactInfoErrors.InvalidEmail(email);
        return normalized;
    }

    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.Length is >= MIN_LENGTH_PHONE and <= MAX_LENGTH_PHONE
            ? digits
            : throw ContactInfoErrors.InvalidPhone(phone);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Email;
        yield return Phone;
    }
}
