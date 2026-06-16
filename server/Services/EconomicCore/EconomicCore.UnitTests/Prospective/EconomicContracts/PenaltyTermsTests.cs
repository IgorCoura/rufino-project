namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public class PenaltyTermsTests
{
    private static Money Brl(decimal amount) => new(amount, Currency.BRL);

    private static PenaltyTerms PercentMonthly(decimal fine, decimal interest)
        => new(PenaltyValueKind.Percent, fine, PenaltyValueKind.Percent, interest, InterestAccrualPeriod.Monthly);

    // Pagamento atrasado dentro do mesmo mês do vencimento cobra só a multa percentual (0 meses completos).
    [Fact]
    public void ComputePenalty_PercentMonthlyWithinDueMonth_ShouldApplyFineOnly()
    {
        var terms = PercentMonthly(0.02m, 0.01m);

        var penalty = terms.ComputePenalty(Brl(1000m), new DateOnly(2025, 10, 20), new DateOnly(2025, 10, 30));

        Assert.Equal(20m, penalty.Amount);
    }

    // Juros percentual mensal acumula por mês-calendário completo: 1000 × (2% + 1%×3) = 50.
    [Fact]
    public void ComputePenalty_PercentMonthlyWithMonthsLate_ShouldApplyFinePlusInterest()
    {
        var terms = PercentMonthly(0.02m, 0.01m);

        var penalty = terms.ComputePenalty(Brl(1000m), new DateOnly(2025, 10, 20), new DateOnly(2026, 1, 5));

        Assert.Equal(50m, penalty.Amount);
    }

    // Juros percentual diário acumula por dia exato de atraso: 1000 × (2% + 0,1%×5) = 25.
    [Fact]
    public void ComputePenalty_PercentDaily_ShouldAccruePerDayLate()
    {
        var terms = new PenaltyTerms(
            PenaltyValueKind.Percent, 0.02m,
            PenaltyValueKind.Percent, 0.001m,
            InterestAccrualPeriod.Daily);

        var penalty = terms.ComputePenalty(Brl(1000m), new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 15));

        Assert.Equal(25m, penalty.Amount);
    }

    // Juros percentual anual acumula por ano-calendário completo: 1000 × (2% + 10%×1) = 120.
    [Fact]
    public void ComputePenalty_PercentYearly_ShouldAccruePerYearLate()
    {
        var terms = new PenaltyTerms(
            PenaltyValueKind.Percent, 0.02m,
            PenaltyValueKind.Percent, 0.10m,
            InterestAccrualPeriod.Yearly);

        var penalty = terms.ComputePenalty(Brl(1000m), new DateOnly(2025, 10, 20), new DateOnly(2026, 2, 1));

        Assert.Equal(120m, penalty.Amount);
    }

    // Multa fixa é valor único na moeda do commitment, independente do tempo de atraso: 50 + 1%×2×1000 = 70.
    [Fact]
    public void ComputePenalty_FixedFine_ShouldChargeOnceRegardlessOfDelay()
    {
        var terms = new PenaltyTerms(
            PenaltyValueKind.FixedAmount, 50m,
            PenaltyValueKind.Percent, 0.01m,
            InterestAccrualPeriod.Monthly);

        var penalty = terms.ComputePenalty(Brl(1000m), new DateOnly(2025, 10, 20), new DateOnly(2025, 12, 1));

        Assert.Equal(70m, penalty.Amount);
        Assert.Equal(Currency.BRL, penalty.Currency);
    }

    // Juros fixo cobra R$ X por unidade decorrida: multa 2%×1000 + 10×3 dias = 50.
    [Fact]
    public void ComputePenalty_FixedInterest_ShouldMultiplyByElapsedUnits()
    {
        var terms = new PenaltyTerms(
            PenaltyValueKind.Percent, 0.02m,
            PenaltyValueKind.FixedAmount, 10m,
            InterestAccrualPeriod.Daily);

        var penalty = terms.ComputePenalty(Brl(1000m), new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 13));

        Assert.Equal(50m, penalty.Amount);
    }

    // Política toda fixa: multa única + juros fixo por mês completo (30 + 15×2 = 60).
    [Fact]
    public void ComputePenalty_AllFixed_ShouldAddFineAndPerUnitInterest()
    {
        var terms = new PenaltyTerms(
            PenaltyValueKind.FixedAmount, 30m,
            PenaltyValueKind.FixedAmount, 15m,
            InterestAccrualPeriod.Monthly);

        var penalty = terms.ComputePenalty(Brl(1000m), new DateOnly(2025, 10, 20), new DateOnly(2025, 12, 1));

        Assert.Equal(60m, penalty.Amount);
    }

    // Pagamento no vencimento ou antes não acumula juros (clamp em zero unidades).
    [Fact]
    public void ComputePenalty_PaidOnDueDate_ShouldChargeFineOnly()
    {
        var terms = PercentMonthly(0.02m, 0.01m);
        var due = new DateOnly(2025, 10, 20);

        var penalty = terms.ComputePenalty(Brl(1000m), due, due);

        Assert.Equal(20m, penalty.Amount);
    }

    // Componente percentual (multa ou juros) fora do intervalo [0,1] lança ECC.CTR30.
    [Theory]
    [InlineData(-0.01, 0.01)]
    [InlineData(1.5, 0.01)]
    [InlineData(0.02, -0.01)]
    [InlineData(0.02, 1.5)]
    public void Constructor_PercentOutsideRange_ShouldThrowECC_CTR30(decimal fine, decimal interest)
    {
        var ex = Assert.Throws<DomainException>(() => PercentMonthly(fine, interest));

        Assert.Equal("ECC.CTR30", ex.Id);
    }

    // Componente de valor fixo negativo (multa ou juros) lança ECC.CTR48.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_NegativeFixedValue_ShouldThrowECC_CTR48(bool negativeOnFine)
    {
        var fineValue = negativeOnFine ? -10m : 30m;
        var interestValue = negativeOnFine ? 10m : -5m;

        var ex = Assert.Throws<DomainException>(() => new PenaltyTerms(
            PenaltyValueKind.FixedAmount, fineValue,
            PenaltyValueKind.FixedAmount, interestValue,
            InterestAccrualPeriod.Monthly));

        Assert.Equal("ECC.CTR48", ex.Id);
    }

    // Kind ou período de acúmulo nulos lançam ECC.CTR49.
    [Fact]
    public void Constructor_NullKindOrPeriod_ShouldThrowECC_CTR49()
    {
        var ex1 = Assert.Throws<DomainException>(() => new PenaltyTerms(
            null!, 0.02m, PenaltyValueKind.Percent, 0.01m, InterestAccrualPeriod.Monthly));
        var ex2 = Assert.Throws<DomainException>(() => new PenaltyTerms(
            PenaltyValueKind.Percent, 0.02m, null!, 0.01m, InterestAccrualPeriod.Monthly));
        var ex3 = Assert.Throws<DomainException>(() => new PenaltyTerms(
            PenaltyValueKind.Percent, 0.02m, PenaltyValueKind.Percent, 0.01m, null!));

        Assert.Equal("ECC.CTR49", ex1.Id);
        Assert.Equal("ECC.CTR49", ex2.Id);
        Assert.Equal("ECC.CTR49", ex3.Id);
    }

    // Igualdade por valor compara os cinco componentes da política.
    [Fact]
    public void Equality_ShouldCompareByComponents()
    {
        var a = PercentMonthly(0.02m, 0.01m);
        var b = PercentMonthly(0.02m, 0.01m);
        var differentKind = new PenaltyTerms(
            PenaltyValueKind.FixedAmount, 0.02m, PenaltyValueKind.Percent, 0.01m, InterestAccrualPeriod.Monthly);
        var differentPeriod = new PenaltyTerms(
            PenaltyValueKind.Percent, 0.02m, PenaltyValueKind.Percent, 0.01m, InterestAccrualPeriod.Daily);

        Assert.Equal(a, b);
        Assert.NotEqual(a, differentKind);
        Assert.NotEqual(a, differentPeriod);
    }
}
