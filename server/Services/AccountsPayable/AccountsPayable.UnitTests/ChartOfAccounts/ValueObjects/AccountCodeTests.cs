namespace AccountsPayable.UnitTests.ChartOfAccounts.ValueObjects;

using AccountsPayable.Domain.ChartOfAccounts.ValueObjects;
using AccountsPayable.Domain.SeedWork;

public class AccountCodeTests
{
    // Códigos válidos: um ou mais grupos de dígitos separados por ponto.
    [Theory]
    [InlineData("4")]
    [InlineData("4.01")]
    [InlineData("4.01.01")]
    [InlineData("1.2.3.4.5")]
    public void Constructor_WithValidFormat_ShouldPreserveValue(string raw)
    {
        var code = new AccountCode(raw);

        Assert.Equal(raw, code.Value);
    }

    // Trim aplicado à entrada antes de validação de formato.
    [Fact]
    public void Constructor_TrimsSurroundingWhitespace()
    {
        var code = new AccountCode("  4.01  ");

        Assert.Equal("4.01", code.Value);
    }

    // Código vazio lança AP.ACO01.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithEmptyInput_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new AccountCode(raw));

        Assert.Equal("AP.ACO01", ex.Id);
    }

    // Formato inválido (letras, pontos consecutivos, leading/trailing dot) lança AP.ACO02.
    [Theory]
    [InlineData("4.A.01")]      // letra
    [InlineData("4..01")]       // ponto duplo
    [InlineData(".4.01")]       // ponto no início
    [InlineData("4.01.")]       // ponto no final
    [InlineData("4-01")]        // separador errado
    public void Constructor_WithInvalidFormat_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new AccountCode(raw));

        Assert.Equal("AP.ACO02", ex.Id);
    }

    // Código acima de MAX_LENGTH lança AP.ACO03.
    [Fact]
    public void Constructor_AboveMaxLength_ShouldThrowDomainException()
    {
        var tooLong = new string('1', AccountCode.MAX_LENGTH + 1);

        var ex = Assert.Throws<DomainException>(() => new AccountCode(tooLong));

        Assert.Equal("AP.ACO03", ex.Id);
    }

    // Dois AccountCodes com mesmo valor são iguais.
    [Fact]
    public void Equals_WithSameValue_ShouldReturnTrue()
    {
        var a = new AccountCode("4.01.01");
        var b = new AccountCode("4.01.01");

        Assert.Equal(a, b);
    }
}
