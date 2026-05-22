namespace AccountsPayable.UnitTests.Payables.ValueObjects;

using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// DigitableLine — linha digitável do boleto (47 dígitos). Aceita separadores comuns
/// (espaço, ponto, hífen) e armazena apenas os dígitos.
/// </summary>
public class DigitableLineTests
{
    // 47 dígitos válidos arbitrários (representação string da linha).
    private const string VALID_47 = "00190000090201200127404200069119978140000050000";

    // String vazia ou só com espaços lança AP.DLN01.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmpty_ShouldThrow_DLN01(string value)
    {
        var ex = Assert.Throws<DomainException>(() => new DigitableLine(value));
        Assert.Equal("AP.DLN01", ex.Id);
    }

    // Caractere não-numérico fora dos separadores tolerados lança AP.DLN03.
    [Fact]
    public void Constructor_NonNumeric_ShouldThrow_DLN03()
    {
        var ex = Assert.Throws<DomainException>(() => new DigitableLine(VALID_47[..46] + "X"));
        Assert.Equal("AP.DLN03", ex.Id);
    }

    // Comprimento diferente de 47 (após remover separadores) lança AP.DLN02.
    [Theory]
    [InlineData("123")]
    [InlineData("123456789012345678901234567890123456789012345678")] // 48
    public void Constructor_InvalidLength_ShouldThrow_DLN02(string raw)
    {
        var ex = Assert.Throws<DomainException>(() => new DigitableLine(raw));
        Assert.Equal("AP.DLN02", ex.Id);
    }

    // 47 dígitos válidos constroem o VO e preservam exatamente o valor.
    [Fact]
    public void Constructor_Valid47Digits_ShouldBuildAndPreserveValue()
    {
        var vo = new DigitableLine(VALID_47);
        Assert.Equal(VALID_47, vo.Value);
        Assert.Equal(DigitableLine.LENGTH, vo.Value.Length);
    }

    // Separadores (ponto, espaço, hífen) são tolerados na entrada e removidos no armazenamento.
    [Fact]
    public void Constructor_WithSeparators_ShouldNormalize()
    {
        var formatted = "00190.00009 02012.001274 04200.069119 9 78140000050000";
        var vo = new DigitableLine(formatted);
        Assert.Equal(VALID_47, vo.Value);
    }

    // Igualdade estrutural por Value.
    [Fact]
    public void Equality_SameValue_ShouldBeEqual()
    {
        Assert.Equal(new DigitableLine(VALID_47), new DigitableLine(VALID_47));
    }
}
