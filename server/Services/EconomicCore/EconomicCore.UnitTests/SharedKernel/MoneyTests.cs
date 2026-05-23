namespace EconomicCore.UnitTests.SharedKernel;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public class MoneyTests
{
    // Construção válida com Currency não-nula preserva Amount arredondado e Currency.
    [Fact]
    public void Constructor_WithValidAmountAndCurrency_ShouldStoreValues()
    {
        var money = new Money(10.50m, Currency.BRL);

        Assert.Equal(10.50m, money.Amount);
        Assert.Same(Currency.BRL, money.Currency);
    }

    // Currency null no construtor lança SHK.MNY01 - CurrencyRequired.
    [Fact]
    public void Constructor_WithNullCurrency_ShouldThrowSHK_MNY01()
    {
        var ex = Assert.Throws<DomainException>(() => new Money(10m, null!));

        Assert.Equal("SHK.MNY01", ex.Id);
    }

    // Banker's rounding (MidpointRounding.ToEven) a 2 casas: 1.005 → 1.00, 1.015 → 1.02, 2.345 → 2.34.
    [Theory]
    [InlineData(1.005, 1.00)]
    [InlineData(1.015, 1.02)]
    [InlineData(2.345, 2.34)]
    [InlineData(2.355, 2.36)]
    [InlineData(0.001, 0.00)]
    [InlineData(0.009, 0.01)]
    public void Constructor_ShouldRoundAmountToTwoDecimalsUsingBankersRounding(double input, double expected)
    {
        var money = new Money((decimal)input, Currency.BRL);

        Assert.Equal((decimal)expected, money.Amount);
    }

    // Zero(Currency) produz Money com Amount=0 e a Currency informada.
    [Fact]
    public void Zero_ShouldProduceMoneyWithAmountZero()
    {
        var zero = Money.Zero(Currency.BRL);

        Assert.Equal(0m, zero.Amount);
        Assert.Same(Currency.BRL, zero.Currency);
        Assert.True(zero.IsZero);
    }

    // Add entre Moneys de mesma Currency soma os Amounts.
    [Fact]
    public void Add_WithSameCurrency_ShouldReturnSummedAmount()
    {
        var a = new Money(10m, Currency.BRL);
        var b = new Money(5.50m, Currency.BRL);

        var sum = a.Add(b);

        Assert.Equal(15.50m, sum.Amount);
        Assert.Same(Currency.BRL, sum.Currency);
    }

    // Subtract entre Moneys de mesma Currency subtrai os Amounts (pode resultar em negativo).
    [Fact]
    public void Subtract_WithSameCurrency_ShouldReturnDifferenceAmount()
    {
        var a = new Money(10m, Currency.BRL);
        var b = new Money(3.50m, Currency.BRL);

        var diff = a.Subtract(b);

        Assert.Equal(6.50m, diff.Amount);
        Assert.Same(Currency.BRL, diff.Currency);
    }

    // Subtract pode produzir Money com Amount negativo (não há validação de sinal).
    [Fact]
    public void Subtract_WhenResultIsNegative_ShouldKeepNegativeAmount()
    {
        var a = new Money(5m, Currency.BRL);
        var b = new Money(10m, Currency.BRL);

        var diff = a.Subtract(b);

        Assert.Equal(-5m, diff.Amount);
        Assert.True(diff.IsNegative);
    }

    // Multiply produz novo Money com Amount escalado pela fator e mesma Currency.
    [Theory]
    [InlineData(10.00, 3, 30.00)]
    [InlineData(10.00, 0.5, 5.00)]
    [InlineData(10.00, 0, 0.00)]
    [InlineData(10.00, -1, -10.00)]
    public void Multiply_ShouldScaleAmountAndPreserveCurrency(double amount, double factor, double expected)
    {
        var money = new Money((decimal)amount, Currency.BRL);

        var result = money.Multiply((decimal)factor);

        Assert.Equal((decimal)expected, result.Amount);
        Assert.Same(Currency.BRL, result.Currency);
    }

    // IsZero/IsPositive/IsNegative refletem o sinal do Amount.
    [Theory]
    [InlineData(0, true, false, false)]
    [InlineData(10, false, true, false)]
    [InlineData(-10, false, false, true)]
    public void SignProperties_ShouldReflectAmountSign(
        double amount, bool isZero, bool isPositive, bool isNegative)
    {
        var money = new Money((decimal)amount, Currency.BRL);

        Assert.Equal(isZero, money.IsZero);
        Assert.Equal(isPositive, money.IsPositive);
        Assert.Equal(isNegative, money.IsNegative);
    }

    // Dois Moneys com mesmo Amount e Currency são iguais (igualdade estrutural).
    [Fact]
    public void Equals_SameAmountAndCurrency_ShouldBeTrue()
    {
        var a = new Money(10.50m, Currency.BRL);
        var b = new Money(10.50m, Currency.BRL);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // Moneys com Amount diferente não são iguais.
    [Fact]
    public void Equals_DifferentAmount_ShouldBeFalse()
    {
        var a = new Money(10m, Currency.BRL);
        var b = new Money(11m, Currency.BRL);

        Assert.NotEqual(a, b);
    }

    // CurrencyMismatch (SHK.MNY02) só pode ser exercitado quando uma segunda Currency existir.
    // Atualmente o catálogo de Currency tem apenas BRL — quando USD/EUR/etc. forem adicionados,
    // habilitar este teste com Add/Subtract entre Moneys de moedas diferentes.
    [Fact(Skip = "Habilitar quando uma segunda Currency for adicionada ao catálogo Smart Enum.")]
    public void Add_WithDifferentCurrency_ShouldThrowSHK_MNY02()
    {
    }
}
