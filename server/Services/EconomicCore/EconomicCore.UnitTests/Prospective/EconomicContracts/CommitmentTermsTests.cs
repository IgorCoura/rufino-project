namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public class CommitmentTermsTests
{
    // Construção válida preserva componentes.
    [Fact]
    public void Constructor_WithValidInputs_ShouldStoreValues()
    {
        var amount = new Money(1000m, Currency.BRL);
        var terms = new CommitmentTerms(amount, tolerancePercent: 0.05m, windowDays: 15);

        Assert.Equal(amount, terms.ExpectedAmount);
        Assert.Equal(0.05m, terms.TolerancePercent);
        Assert.Equal(15, terms.WindowDays);
    }

    // ExpectedAmount nulo ou ≤ 0 lança ECC.CTR08.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveAmount_ShouldThrowECC_CTR08(double amount)
    {
        var ex = Assert.Throws<DomainException>(
            () => new CommitmentTerms(new Money((decimal)amount, Currency.BRL), 0.05m, 15));

        Assert.Equal("ECC.CTR08", ex.Id);
    }

    // TolerancePercent fora de [0..1] lança ECC.CTR09.
    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void Constructor_WithToleranceOutOfRange_ShouldThrowECC_CTR09(double tolerance)
    {
        var ex = Assert.Throws<DomainException>(
            () => new CommitmentTerms(new Money(1000m, Currency.BRL), (decimal)tolerance, 15));

        Assert.Equal("ECC.CTR09", ex.Id);
    }

    // WindowDays negativo lança ECC.CTR10.
    [Fact]
    public void Constructor_WithNegativeWindow_ShouldThrowECC_CTR10()
    {
        var ex = Assert.Throws<DomainException>(
            () => new CommitmentTerms(new Money(1000m, Currency.BRL), 0.05m, windowDays: -1));

        Assert.Equal("ECC.CTR10", ex.Id);
    }

    // IsWithinTolerance compara diferença absoluta vs. ExpectedAmount × TolerancePercent.
    [Theory]
    [InlineData(1000, 0.05, 1050, true)]   // dentro: 50/1000 = 5%
    [InlineData(1000, 0.05, 950, true)]    // dentro
    [InlineData(1000, 0.05, 1051, false)]  // 1 acima da tolerância
    [InlineData(1000, 0.10, 1100, true)]   // dentro
    [InlineData(1000, 0.10, 900, true)]    // dentro
    [InlineData(1000, 0.00, 1000, true)]   // tolerância zero exige exato
    [InlineData(1000, 0.00, 1001, false)]
    public void IsWithinTolerance_ShouldRespectExpectedAndPercent(double expected, double tolerance, double actual, bool result)
    {
        var terms = new CommitmentTerms(new Money((decimal)expected, Currency.BRL), (decimal)tolerance, 15);

        Assert.Equal(result, terms.IsWithinTolerance(new Money((decimal)actual, Currency.BRL)));
    }
}
