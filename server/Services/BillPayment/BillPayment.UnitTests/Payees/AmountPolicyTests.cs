namespace BillPayment.UnitTests.Payees;

using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Payees.Mothers;

public class AmountPolicyTests
{
    // From monta a política fixa a partir dos valores crus do cadastro, em BRL.
    [Fact]
    public void From_WithFixedKind_ShouldBuildFixedPolicyInBrl()
    {
        var policy = AmountPolicy.From(AmountPolicyKind.Fixed, 1500m, 5m, null, null);

        Assert.Same(AmountPolicyKind.Fixed, policy.Kind);
        Assert.Equal(1500m, policy.ExpectedAmount!.Amount);
        Assert.Same(Currency.BRL, policy.ExpectedAmount.Currency);
        Assert.Equal(5m, policy.TolerancePercent);
    }

    // From monta a faixa a partir dos valores crus, com mínimo e máximo na mesma moeda.
    [Fact]
    public void From_WithRangeKind_ShouldBuildRangePolicyInBrl()
    {
        var policy = AmountPolicy.From(AmountPolicyKind.Range, null, null, 100m, 900m);

        Assert.Same(AmountPolicyKind.Range, policy.Kind);
        Assert.Equal(100m, policy.MinAmount!.Amount);
        Assert.Equal(900m, policy.MaxAmount!.Amount);
        Assert.Same(Currency.BRL, policy.MinAmount.Currency);
    }

    // From ignora os valores quando o tipo é Unbounded — não há expectativa a guardar.
    [Fact]
    public void From_WithUnboundedKind_ShouldIgnoreAmounts()
    {
        var policy = AmountPolicy.From(AmountPolicyKind.Unbounded, 1500m, 5m, 100m, 900m);

        Assert.Same(AmountPolicyKind.Unbounded, policy.Kind);
        Assert.Null(policy.ExpectedAmount);
        Assert.Null(policy.MinAmount);
        Assert.Null(policy.MaxAmount);
    }

    // Cada tipo exige seus próprios campos; faltando qualquer um, reprova em BLP.PYE07.
    // Os valores viajam como double porque atributo de teste não aceita literal decimal.
    [Theory]
    [InlineData("Fixed", null, 5d, null, null)]
    [InlineData("Fixed", 1500d, null, null, null)]
    [InlineData("Range", null, null, null, 900d)]
    [InlineData("Range", null, null, 100d, null)]
    public void From_WithMissingRequiredAmounts_ShouldThrow_BLP_PYE07(
        string kindName,
        double? expectedAmount,
        double? tolerancePercent,
        double? minAmount,
        double? maxAmount)
    {
        var kind = Enumeration.FromDisplayName<AmountPolicyKind>(kindName);

        var ex = Assert.Throws<DomainException>(() => AmountPolicy.From(
            kind,
            (decimal?)expectedAmount,
            (decimal?)tolerancePercent,
            (decimal?)minAmount,
            (decimal?)maxAmount));

        Assert.Equal("BLP.PYE07", ex.Id);
    }

    // Tipo de política ausente é recusado — BLP.PYE06.
    [Fact]
    public void From_WithoutKind_ShouldThrow_BLP_PYE06()
    {
        var ex = Assert.Throws<DomainException>(() => AmountPolicy.From(null!, 1500m, 5m, null, null));

        Assert.Equal("BLP.PYE06", ex.Id);
    }

    // From não afrouxa as invariantes das factories: valor fixo não positivo continua reprovado.
    [Fact]
    public void From_WithNonPositiveFixedAmount_ShouldThrow_BLP_PYE08()
    {
        var ex = Assert.Throws<DomainException>(
            () => AmountPolicy.From(AmountPolicyKind.Fixed, 0m, 5m, null, null));

        Assert.Equal("BLP.PYE08", ex.Id);
    }

    // Política de valor fixo guarda o valor esperado, a tolerância e é conclusiva.
    [Fact]
    public void Fixed_WithValidAmount_ShouldStoreExpectationAndBeConclusive()
    {
        var policy = AmountPolicy.Fixed(PayeeMother.Brl(1000m), 5m);

        Assert.Same(AmountPolicyKind.Fixed, policy.Kind);
        Assert.Equal(1000m, policy.ExpectedAmount!.Amount);
        Assert.Equal(5m, policy.TolerancePercent);
        Assert.True(policy.IsConclusive);
    }

    // Valor esperado precisa ser positivo — BLP.PYE08.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Fixed_WithNonPositiveAmount_ShouldThrow_BLP_PYE08(decimal amount)
    {
        var ex = Assert.Throws<DomainException>(
            () => AmountPolicy.Fixed(PayeeMother.Brl(amount), 5m));

        Assert.Equal("BLP.PYE08", ex.Id);
    }

    // Tolerância fora de 0..100 é recusada — BLP.PYE09.
    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public void Fixed_WithToleranceOutOfRange_ShouldThrow_BLP_PYE09(decimal tolerance)
    {
        var ex = Assert.Throws<DomainException>(
            () => AmountPolicy.Fixed(PayeeMother.Brl(1000m), tolerance));

        Assert.Equal("BLP.PYE09", ex.Id);
    }

    // Os extremos da tolerância (0% e 100%) são válidos.
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Fixed_WithBoundaryTolerance_ShouldBeAccepted(decimal tolerance)
    {
        var policy = AmountPolicy.Fixed(PayeeMother.Brl(1000m), tolerance);

        Assert.Equal(tolerance, policy.TolerancePercent);
    }

    // Valor sem política informada é recusado — BLP.PYE07.
    [Fact]
    public void Fixed_WithoutAmount_ShouldThrow_BLP_PYE07()
    {
        var ex = Assert.Throws<DomainException>(() => AmountPolicy.Fixed(null!, 5m));

        Assert.Equal("BLP.PYE07", ex.Id);
    }

    // Valor dentro da tolerância casa; fora dela, não. Tolerância de 10% sobre 1000 → [900, 1100].
    [Theory]
    [InlineData(1000, true)]
    [InlineData(900, true)]
    [InlineData(1100, true)]
    [InlineData(899.99, false)]
    [InlineData(1100.01, false)]
    public void Matches_WithFixedPolicy_ShouldRespectTolerance(decimal actual, bool expected)
    {
        var policy = AmountPolicy.Fixed(PayeeMother.Brl(1000m), 10m);

        Assert.Equal(expected, policy.Matches(PayeeMother.Brl(actual)));
    }

    // Tolerância zero exige valor exato.
    [Theory]
    [InlineData(1000, true)]
    [InlineData(1000.01, false)]
    public void Matches_WithZeroTolerance_ShouldRequireExactAmount(decimal actual, bool expected)
    {
        var policy = AmountPolicy.Fixed(PayeeMother.Brl(1000m), 0m);

        Assert.Equal(expected, policy.Matches(PayeeMother.Brl(actual)));
    }

    // Faixa guarda os limites e é conclusiva.
    [Fact]
    public void Range_WithValidBounds_ShouldStoreBoundsAndBeConclusive()
    {
        var policy = AmountPolicy.Range(PayeeMother.Brl(100m), PayeeMother.Brl(500m));

        Assert.Same(AmountPolicyKind.Range, policy.Kind);
        Assert.Equal(100m, policy.MinAmount!.Amount);
        Assert.Equal(500m, policy.MaxAmount!.Amount);
        Assert.True(policy.IsConclusive);
    }

    // Faixa com mínimo maior que o máximo é recusada — BLP.PYE10.
    [Fact]
    public void Range_WithInvertedBounds_ShouldThrow_BLP_PYE10()
    {
        var ex = Assert.Throws<DomainException>(
            () => AmountPolicy.Range(PayeeMother.Brl(500m), PayeeMother.Brl(100m)));

        Assert.Equal("BLP.PYE10", ex.Id);
    }

    // Limites negativos são recusados — BLP.PYE11.
    [Fact]
    public void Range_WithNegativeBound_ShouldThrow_BLP_PYE11()
    {
        var ex = Assert.Throws<DomainException>(
            () => AmountPolicy.Range(PayeeMother.Brl(-1m), PayeeMother.Brl(100m)));

        Assert.Equal("BLP.PYE11", ex.Id);
    }

    // Faixa com limite ausente é recusada — BLP.PYE07.
    [Fact]
    public void Range_WithoutBound_ShouldThrow_BLP_PYE07()
    {
        var ex = Assert.Throws<DomainException>(
            () => AmountPolicy.Range(PayeeMother.Brl(100m), null!));

        Assert.Equal("BLP.PYE07", ex.Id);
    }

    // Faixa com mínimo igual ao máximo é válida — equivale a valor exato.
    [Fact]
    public void Range_WithEqualBounds_ShouldBeAccepted()
    {
        var policy = AmountPolicy.Range(PayeeMother.Brl(100m), PayeeMother.Brl(100m));

        Assert.True(policy.Matches(PayeeMother.Brl(100m)));
    }

    // Valor dentro da faixa (inclusive nos extremos) casa; fora dela, não.
    [Theory]
    [InlineData(100, true)]
    [InlineData(300, true)]
    [InlineData(500, true)]
    [InlineData(99.99, false)]
    [InlineData(500.01, false)]
    public void Matches_WithRangePolicy_ShouldBeInclusiveOnBounds(decimal actual, bool expected)
    {
        var policy = AmountPolicy.Range(PayeeMother.Brl(100m), PayeeMother.Brl(500m));

        Assert.Equal(expected, policy.Matches(PayeeMother.Brl(actual)));
    }

    // Sem expectativa, qualquer valor "casa" — mas a política não é conclusiva, então o check sai inconclusivo.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(999999)]
    public void Matches_WithUnboundedPolicy_ShouldAlwaysMatchButNotBeConclusive(decimal actual)
    {
        var policy = AmountPolicy.Unbounded();

        Assert.True(policy.Matches(PayeeMother.Brl(actual)));
        Assert.False(policy.IsConclusive);
    }

    // Valor ausente nunca casa com política alguma.
    [Fact]
    public void Matches_WithNullAmount_ShouldReturnFalse()
    {
        Assert.False(AmountPolicy.Unbounded().Matches(null!));
        Assert.False(AmountPolicy.Fixed(PayeeMother.Brl(10m), 0m).Matches(null!));
    }

    // Igualdade é por valor: duas políticas fixas idênticas são iguais.
    [Fact]
    public void Equals_WithSameConfiguration_ShouldBeTrue()
    {
        var a = AmountPolicy.Fixed(PayeeMother.Brl(1000m), 5m);
        var b = AmountPolicy.Fixed(PayeeMother.Brl(1000m), 5m);

        Assert.Equal(a, b);
    }

    // Políticas de naturezas diferentes não são iguais.
    [Fact]
    public void Equals_WithDifferentKind_ShouldBeFalse()
    {
        Assert.NotEqual(AmountPolicy.Unbounded(), AmountPolicy.Fixed(PayeeMother.Brl(10m), 0m));
    }
}
