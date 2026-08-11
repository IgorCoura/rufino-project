namespace BillPayment.UnitTests.SharedKernel;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

public class BankCodeTests
{
    // Código de três dígitos é aceito e guardado como veio.
    [Theory]
    [InlineData("341")]
    [InlineData("033")]
    [InlineData("237")]
    [InlineData("001")]
    public void Constructor_WithThreeDigits_ShouldStoreValue(string value)
    {
        var code = new BankCode(value);

        Assert.Equal(value, code.Value);
    }

    // Código com menos de três dígitos é normalizado com zeros à esquerda — "33" e "033" são o mesmo banco.
    [Theory]
    [InlineData("33", "033")]
    [InlineData("1", "001")]
    [InlineData(" 33 ", "033")]
    public void Constructor_WithShortCode_ShouldPadWithLeadingZeros(string input, string expected)
    {
        var code = new BankCode(input);

        Assert.Equal(expected, code.Value);
    }

    // Separadores comuns são descartados antes da normalização.
    [Fact]
    public void Constructor_WithFormattingCharacters_ShouldStripThem()
    {
        var code = new BankCode("3-4-1");

        Assert.Equal("341", code.Value);
    }

    // Valor vazio ou só espaços é recusado — SHK.BNK01.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithBlankValue_ShouldThrow_SHK_BNK01(string? value)
    {
        var ex = Assert.Throws<DomainException>(() => new BankCode(value!));

        Assert.Equal("SHK.BNK01", ex.Id);
    }

    // Valor sem dígitos, com mais de três dígitos, ou igual a 000 é recusado — SHK.BNK02.
    [Theory]
    [InlineData("abc")]
    [InlineData("3411")]
    [InlineData("000")]
    [InlineData("0")]
    public void Constructor_WithInvalidCode_ShouldThrow_SHK_BNK02(string value)
    {
        var ex = Assert.Throws<DomainException>(() => new BankCode(value));

        Assert.Equal("SHK.BNK02", ex.Id);
    }

    // Igualdade é por valor normalizado: "33" equivale a "033".
    [Fact]
    public void Equals_WithSameNormalizedValue_ShouldBeTrue()
    {
        Assert.Equal(new BankCode("033"), new BankCode("33"));
    }

    // Códigos de bancos diferentes não são iguais.
    [Fact]
    public void Equals_WithDifferentValue_ShouldBeFalse()
    {
        Assert.NotEqual(new BankCode("341"), new BankCode("237"));
    }
}
