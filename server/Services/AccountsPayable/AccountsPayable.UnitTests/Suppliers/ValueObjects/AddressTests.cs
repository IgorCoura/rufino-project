namespace AccountsPayable.UnitTests.Suppliers.ValueObjects;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.ValueObjects;

public class AddressTests
{
    private static Address ValidAddress(
        string street = "Av. Paulista",
        string number = "1000",
        string? complement = "Sala 100",
        string neighborhood = "Bela Vista",
        string city = "São Paulo",
        string state = "SP",
        string zipCode = "01310-100")
        => new(street, number, complement, neighborhood, city, state, zipCode);

    // Endereço válido completo é construído preservando os campos (trim + UF maiúsculas + CEP digit-only).
    [Fact]
    public void Constructor_WithValidData_ShouldPreserveFields()
    {
        var address = ValidAddress();

        Assert.Equal("Av. Paulista", address.Street);
        Assert.Equal("1000", address.Number);
        Assert.Equal("Sala 100", address.Complement);
        Assert.Equal("Bela Vista", address.Neighborhood);
        Assert.Equal("São Paulo", address.City);
        Assert.Equal("SP", address.State);
        Assert.Equal("01310100", address.ZipCode);
    }

    // Complemento vazio ou whitespace é convertido para null (opcional).
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyComplement_ShouldStoreNull(string? complement)
    {
        var address = ValidAddress(complement: complement);

        Assert.Null(address.Complement);
    }

    // UF é normalizado para maiúsculas mesmo se passado em minúsculas.
    [Fact]
    public void Constructor_WithLowercaseState_ShouldUppercaseIt()
    {
        var address = ValidAddress(state: "sp");

        Assert.Equal("SP", address.State);
    }

    // Cada campo obrigatório vazio lança AP.ADR01.
    [Theory]
    [InlineData("street")]
    [InlineData("number")]
    [InlineData("neighborhood")]
    [InlineData("city")]
    public void Constructor_WithEmptyRequiredField_ShouldThrowDomainException(string field)
    {
        DomainException ex = field switch
        {
            "street" => Assert.Throws<DomainException>(() => ValidAddress(street: " ")),
            "number" => Assert.Throws<DomainException>(() => ValidAddress(number: " ")),
            "neighborhood" => Assert.Throws<DomainException>(() => ValidAddress(neighborhood: " ")),
            "city" => Assert.Throws<DomainException>(() => ValidAddress(city: " ")),
            _ => throw new InvalidOperationException()
        };

        Assert.Equal("AP.ADR01", ex.Id);
    }

    // Cada campo acima do max length lança AP.ADR02.
    [Fact]
    public void Constructor_WithStreetTooLong_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(
            () => ValidAddress(street: new string('A', Address.STREET_MAX_LENGTH + 1)));

        Assert.Equal("AP.ADR02", ex.Id);
    }

    // UF com tamanho diferente de 2 letras lança AP.ADR03.
    [Theory]
    [InlineData("X")]
    [InlineData("ABC")]
    [InlineData("12")]
    public void Constructor_WithInvalidState_ShouldThrowDomainException(string state)
    {
        var ex = Assert.Throws<DomainException>(() => ValidAddress(state: state));

        Assert.Equal("AP.ADR03", ex.Id);
    }

    // CEP com número de dígitos diferente de 8 lança AP.ADR04.
    [Theory]
    [InlineData("123")]
    [InlineData("123456789")]
    [InlineData("abcdefgh")]
    public void Constructor_WithInvalidZipCode_ShouldThrowDomainException(string zip)
    {
        var ex = Assert.Throws<DomainException>(() => ValidAddress(zipCode: zip));

        Assert.Equal("AP.ADR04", ex.Id);
    }

    // Dois endereços com mesmos campos são iguais (igualdade composta).
    [Fact]
    public void Equals_WithSameFields_ShouldReturnTrue()
    {
        var a = ValidAddress();
        var b = ValidAddress();

        Assert.Equal(a, b);
    }
}
