namespace AccountsPayable.Domain.Suppliers.ValueObjects;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

public sealed class Address : ValueObject
{
    public const int STREET_MAX_LENGTH = 200;
    public const int NUMBER_MAX_LENGTH = 20;
    public const int COMPLEMENT_MAX_LENGTH = 100;
    public const int NEIGHBORHOOD_MAX_LENGTH = 100;
    public const int CITY_MAX_LENGTH = 100;
    public const int STATE_LENGTH = 2;     // UF (e.g., SP)
    public const int ZIP_CODE_LENGTH = 8;  // CEP digits only

    public string Street { get; }
    public string Number { get; }
    public string? Complement { get; }
    public string Neighborhood { get; }
    public string City { get; }
    public string State { get; }
    public string ZipCode { get; }

    public Address(
        string street,
        string number,
        string? complement,
        string neighborhood,
        string city,
        string state,
        string zipCode)
    {
        Street = RequireFitted(street, nameof(Street), STREET_MAX_LENGTH);
        Number = RequireFitted(number, nameof(Number), NUMBER_MAX_LENGTH);
        Complement = string.IsNullOrWhiteSpace(complement)
            ? null
            : FitOptional(complement, nameof(Complement), COMPLEMENT_MAX_LENGTH);
        Neighborhood = RequireFitted(neighborhood, nameof(Neighborhood), NEIGHBORHOOD_MAX_LENGTH);
        City = RequireFitted(city, nameof(City), CITY_MAX_LENGTH);
        State = NormalizeState(state);
        ZipCode = NormalizeZipCode(zipCode);
    }

    private static string RequireFitted(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw AddressErrors.FieldEmpty(fieldName);
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw AddressErrors.FieldTooLong(fieldName, maxLength);
        return trimmed;
    }

    private static string FitOptional(string value, string fieldName, int maxLength)
    {
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw AddressErrors.FieldTooLong(fieldName, maxLength);
        return trimmed;
    }

    private static string NormalizeState(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            throw AddressErrors.FieldEmpty(nameof(State));
        var trimmed = state.Trim().ToUpperInvariant();
        if (trimmed.Length != STATE_LENGTH || !trimmed.All(char.IsLetter))
            throw AddressErrors.InvalidState(state);
        return trimmed;
    }

    private static string NormalizeZipCode(string zipCode)
    {
        if (string.IsNullOrWhiteSpace(zipCode))
            throw AddressErrors.FieldEmpty(nameof(ZipCode));
        var digits = new string(zipCode.Where(char.IsDigit).ToArray());
        if (digits.Length != ZIP_CODE_LENGTH)
            throw AddressErrors.InvalidZipCode(zipCode);
        return digits;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return Number;
        yield return Complement;
        yield return Neighborhood;
        yield return City;
        yield return State;
        yield return ZipCode;
    }
}
