namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public class PenaltyTermsTests
{
    // Sem atraso em meses cheios (monthsLate=0) só a multa incide: 1000 × 2% = 20.
    [Fact]
    public void ComputePenalty_WithZeroMonthsLate_ShouldApplyFineOnly()
    {
        var terms = new PenaltyTerms(finePercent: 0.02m, monthlyInterestPercent: 0.01m);

        var penalty = terms.ComputePenalty(new Money(1000m, Currency.BRL), monthsLate: 0);

        Assert.Equal(20m, penalty.Amount);
    }

    // Multa + juros de mora por mês: 1000 × (2% + 1%×3) = 50.
    [Fact]
    public void ComputePenalty_WithMonthsLate_ShouldApplyFinePlusInterest()
    {
        var terms = new PenaltyTerms(finePercent: 0.02m, monthlyInterestPercent: 0.01m);

        var penalty = terms.ComputePenalty(new Money(1000m, Currency.BRL), monthsLate: 3);

        Assert.Equal(50m, penalty.Amount);
    }

    // Percentual fora de [0,1] é rejeitado com ECC.CTR30 (testa multa e juros).
    [Theory]
    [InlineData(-0.01, 0.01)]
    [InlineData(1.5, 0.01)]
    [InlineData(0.02, -0.01)]
    [InlineData(0.02, 1.5)]
    public void Constructor_WithPercentOutsideRange_ShouldThrowECC_CTR30(decimal fine, decimal interest)
    {
        var ex = Assert.Throws<DomainException>(() => new PenaltyTerms(fine, interest));

        Assert.Equal("ECC.CTR30", ex.Id);
    }

    // monthsLate negativo é tratado como zero (defensivo): incide só a multa.
    [Fact]
    public void ComputePenalty_WithNegativeMonths_ShouldClampToFineOnly()
    {
        var terms = new PenaltyTerms(0.02m, 0.01m);

        var penalty = terms.ComputePenalty(new Money(1000m, Currency.BRL), monthsLate: -5);

        Assert.Equal(20m, penalty.Amount);
    }

    // Igualdade por valor: mesmas taxas ⇒ iguais; taxa diferente ⇒ diferentes.
    [Fact]
    public void Equality_ShouldCompareByComponents()
    {
        Assert.Equal(new PenaltyTerms(0.02m, 0.01m), new PenaltyTerms(0.02m, 0.01m));
        Assert.NotEqual(new PenaltyTerms(0.02m, 0.01m), new PenaltyTerms(0.03m, 0.01m));
    }
}
