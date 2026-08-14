namespace TenantManagement.Domain.SharedKernel;

using TenantManagement.Domain.SeedWork;

/// <summary>
/// Endereço do tenant. Obrigatório no cadastro porque a abertura da subconta no provedor
/// de pagamento o exige para pessoa física e jurídica — deixá-lo opcional adiaria a
/// descoberta de que metade da base não o tem para o dia em que o dinheiro precisa andar.
/// </summary>
/// <remarks>
/// A validação inteira mora no construtor, e não em setters que lançam: um objeto que
/// falha no quinto campo depois de ter atribuído quatro é um meio-endereço válido para
/// o compilador e inválido para o negócio.
/// </remarks>
public sealed class Address : ValueObject
{
    public const int ZIP_CODE_LENGTH = 8;
    public const int STATE_LENGTH = 2;
    public const int MAX_LENGTH_STREET = 100;
    public const int MAX_LENGTH_NUMBER = 10;
    public const int MAX_LENGTH_COMPLEMENT = 50;
    public const int MAX_LENGTH_NEIGHBORHOOD = 50;
    public const int MAX_LENGTH_CITY = 50;
    public const int MAX_LENGTH_COUNTRY = 50;

    private const string DEFAULT_COUNTRY = "BRASIL";

    public string ZipCode { get; private set; } = string.Empty;
    public string Street { get; private set; } = string.Empty;
    public string Number { get; private set; } = string.Empty;

    /// <summary>Único campo opcional: nem todo endereço tem bloco, sala ou apartamento.</summary>
    public string Complement { get; private set; } = string.Empty;
    public string Neighborhood { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;

    private Address() { }

    public static Address Create(
        string zipCode,
        string street,
        string number,
        string? complement,
        string neighborhood,
        string city,
        string state,
        string? country = null)
        => new()
        {
            ZipCode = NormalizeZipCode(zipCode),
            Street = Required(street, nameof(Street), MAX_LENGTH_STREET),
            Number = Required(number, nameof(Number), MAX_LENGTH_NUMBER),
            Complement = Optional(complement, nameof(Complement), MAX_LENGTH_COMPLEMENT),
            Neighborhood = Required(neighborhood, nameof(Neighborhood), MAX_LENGTH_NEIGHBORHOOD),
            City = Required(city, nameof(City), MAX_LENGTH_CITY),
            State = NormalizeState(state),
            Country = Optional(country, nameof(Country), MAX_LENGTH_COUNTRY) is { Length: > 0 } informed
                ? informed
                : DEFAULT_COUNTRY,
        };

    public string FormattedZipCode() => $"{ZipCode[..5]}-{ZipCode[5..]}";

    private static string NormalizeZipCode(string zipCode)
    {
        var digits = new string((zipCode ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length == ZIP_CODE_LENGTH ? digits : throw AddressErrors.InvalidZipCode(zipCode ?? string.Empty);
    }

    // A UF fica presa em duas letras de propósito: aceitar "São Paulo" ao lado de "SP" é como
    // a mesma unidade federativa passa a existir em três formas na base.
    private static string NormalizeState(string state)
    {
        var normalized = (state ?? string.Empty).Trim().ToUpperInvariant();
        return normalized.Length == STATE_LENGTH && normalized.All(char.IsAsciiLetterUpper)
            ? normalized
            : throw AddressErrors.InvalidState(state ?? string.Empty);
    }

    private static string Required(string value, string fieldName, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length == 0)
            throw AddressErrors.FieldRequired(fieldName);
        if (normalized.Length > maxLength)
            throw AddressErrors.FieldTooLong(fieldName, maxLength);
        return normalized;
    }

    private static string Optional(string? value, string fieldName, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length > maxLength)
            throw AddressErrors.FieldTooLong(fieldName, maxLength);
        return normalized;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ZipCode;
        yield return Street;
        yield return Number;
        yield return Complement;
        yield return Neighborhood;
        yield return City;
        yield return State;
        yield return Country;
    }
}
