namespace AccountsPayable.UnitTests.Payables.ValueObjects;

using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;

public class MoneyTests
{
    // Money construído com valor positivo arredonda a 2 casas (banker's rounding) e mantém a moeda.
    [Theory]
    [InlineData(100.00, 100.00)]
    [InlineData(1.005, 1.00)]   // banker's: arredonda para par
    [InlineData(1.015, 1.02)]
    [InlineData(0.01, 0.01)]    // mínimo positivo
    public void Constructor_WithPositiveAmount_ShouldRoundToTwoDecimals(decimal raw, decimal expected)
    {
        var money = new Money(raw, Currency.Brl);

        Assert.Equal(expected, money.Amount);
        Assert.Equal(Currency.Brl, money.Currency);
    }

    // Valor zero ou negativo é rejeitado com AP.MON02 (Money sempre estritamente positivo).
    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Constructor_WithNonPositiveAmount_ShouldThrowDomainException(decimal amount)
    {
        var ex = Assert.Throws<DomainException>(() => new Money(amount, Currency.Brl));

        Assert.Equal("AP.MON02", ex.Id);
    }

    // Currency null lança AP.MON01.
    [Fact]
    public void Constructor_WithNullCurrency_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new Money(100m, null!));

        Assert.Equal("AP.MON01", ex.Id);
    }

    // Dois Money com mesmo valor e moeda são iguais (igualdade por componente).
    [Fact]
    public void Equals_WithSameAmountAndCurrency_ShouldReturnTrue()
    {
        var a = new Money(100m, Currency.Brl);
        var b = new Money(100m, Currency.Brl);

        Assert.Equal(a, b);
    }

    // Money de mesmas amount mas moedas distintas (BRL vs USD) não são iguais.
    [Fact]
    public void Equals_WithDifferentCurrency_ShouldReturnFalse()
    {
        var brl = new Money(100m, Currency.Brl);
        var usd = new Money(100m, Currency.Usd);

        Assert.NotEqual(brl, usd);
    }
}
