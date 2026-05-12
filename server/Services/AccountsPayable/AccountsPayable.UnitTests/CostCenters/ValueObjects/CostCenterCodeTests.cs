namespace AccountsPayable.UnitTests.CostCenters.ValueObjects;

using AccountsPayable.Domain.CostCenters.ValueObjects;
using AccountsPayable.Domain.SeedWork;

public class CostCenterCodeTests
{
    // Códigos válidos aceitam letras, dígitos, hífen e underscore; são normalizados para uppercase.
    [Theory]
    [InlineData("OBRA-SYRAH", "OBRA-SYRAH")]
    [InlineData("obra-syrah", "OBRA-SYRAH")]
    [InlineData("ESCRITORIO", "ESCRITORIO")]
    [InlineData("filial_01", "FILIAL_01")]
    [InlineData("XY", "XY")]
    public void Constructor_WithValidInput_ShouldUpperCaseAndPreserve(string raw, string expected)
    {
        var code = new CostCenterCode(raw);

        Assert.Equal(expected, code.Value);
    }

    // Código vazio lança AP.CCC01.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_WithEmptyInput_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new CostCenterCode(raw));

        Assert.Equal("AP.CCC01", ex.Id);
    }

    // Código com menos do que MIN_LENGTH lança AP.CCC02.
    [Fact]
    public void Constructor_BelowMinLength_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new CostCenterCode("X"));

        Assert.Equal("AP.CCC02", ex.Id);
    }

    // Código acima de MAX_LENGTH lança AP.CCC03.
    [Fact]
    public void Constructor_AboveMaxLength_ShouldThrowDomainException()
    {
        var tooLong = new string('A', CostCenterCode.MAX_LENGTH + 1);

        var ex = Assert.Throws<DomainException>(() => new CostCenterCode(tooLong));

        Assert.Equal("AP.CCC03", ex.Id);
    }

    // Caracteres não aceitos (espaço no meio, símbolo, ponto, acento) lançam AP.CCC04.
    [Theory]
    [InlineData("OBRA SYRAH")]   // espaço
    [InlineData("OBRA.SYRAH")]   // ponto
    [InlineData("OBRA@SYRAH")]   // símbolo
    [InlineData("OBRA/SYRAH")]   // barra
    public void Constructor_WithInvalidCharacters_ShouldThrowDomainException(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new CostCenterCode(raw));

        Assert.Equal("AP.CCC04", ex.Id);
    }
}
