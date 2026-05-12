namespace AccountsPayable.UnitTests.Suppliers.ValueObjects;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.ValueObjects;

public class PhoneNumberTests
{
    // Telefone com formatação variada é normalizado para apenas dígitos.
    [Theory]
    [InlineData("(11) 98765-4321", "11987654321")]      // 11 dígitos (celular)
    [InlineData("11987654321", "11987654321")]
    [InlineData("(11) 3456-7890", "1134567890")]        // 10 dígitos (fixo)
    [InlineData("11 3456-7890", "1134567890")]
    public void Constructor_WithFormattedPhone_ShouldNormalizeToDigits(string raw, string expected)
    {
        var phone = new PhoneNumber(raw);

        Assert.Equal(expected, phone.Value);
    }

    // Telefone vazio lança AP.PHN01.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithEmptyInput_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new PhoneNumber(raw));

        Assert.Equal("AP.PHN01", ex.Id);
    }

    // Telefone fora da faixa permitida (10-11 dígitos) lança AP.PHN02.
    [Theory]
    [InlineData("123456789")]          // 9 dígitos
    [InlineData("123456789012")]       // 12 dígitos
    public void Constructor_WithInvalidLength_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new PhoneNumber(raw));

        Assert.Equal("AP.PHN02", ex.Id);
    }
}
