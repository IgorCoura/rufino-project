namespace BillPayment.UnitTests.Lookups;

using BillPayment.Domain.Lookups;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Lookups.Mothers;

public class LookupSnapshotTests
{
    // O retrato completo da cobrança bancária guarda tudo que os checks consomem.
    [Fact]
    public void Create_ForBankSlip_ShouldKeepEveryConsultedField()
    {
        var snapshot = LookupMother.BankSlip();

        Assert.Equal(LookupMother.BENEFICIARY_CNPJ, snapshot.Beneficiary.TaxId!.Value);
        Assert.Equal("341", snapshot.BankCode!.Value);
        Assert.Equal(150.00m, snapshot.Amount!.Amount);
        Assert.Equal(LookupMother.DueDate, snapshot.DueDate);
        Assert.Equal(LookupMother.ConsultedAt, snapshot.ConsultedAt);
    }

    // Arrecadação constrói o retrato sem documento, sem banco e sem vencimento — a cobertura
    // medida na sprint 1.0. Exigir esses campos tornaria o VO inconstruível para 100% do tipo.
    [Fact]
    public void Create_ForUtilityBill_ShouldBuildWithoutTaxIdBankOrDueDate()
    {
        var snapshot = LookupMother.Utility();

        Assert.False(snapshot.Beneficiary.HasTaxId);
        Assert.Null(snapshot.BankCode);
        Assert.Null(snapshot.DueDate);
        Assert.Equal(LookupMother.UTILITY_COMPANY_NAME, snapshot.Beneficiary.DisplayName);
    }

    // Sem beneficiário não há retrato — BLP.LKP02.
    [Fact]
    public void Create_WithoutBeneficiary_ShouldThrow_BLP_LKP02()
    {
        var ex = Assert.Throws<DomainException>(
            () => LookupSnapshot.Create(null!, LookupMother.ConsultedAt));

        Assert.Equal("BLP.LKP02", ex.Id);
    }

    // Sem instante de consulta a evidência não tem validade nem prazo de validade — BLP.LKP07.
    [Fact]
    public void Create_WithoutConsultedAt_ShouldThrow_BLP_LKP07()
    {
        var ex = Assert.Throws<DomainException>(
            () => LookupSnapshot.Create(LookupParty.Of("Beneficiário"), default));

        Assert.Equal("BLP.LKP07", ex.Id);
    }

    // Faixa de valor invertida é resposta incoerente do provedor e não vira retrato — BLP.LKP05.
    [Fact]
    public void Create_WithInvertedAmountBounds_ShouldThrow_BLP_LKP05()
    {
        var ex = Assert.Throws<DomainException>(() => LookupSnapshot.Create(
            LookupParty.Of("Beneficiário"),
            LookupMother.ConsultedAt,
            minAmount: LookupMother.Brl(100m),
            maxAmount: LookupMother.Brl(10m)));

        Assert.Equal("BLP.LKP05", ex.Id);
    }

    // Valor fechado e presente: o check de valor tem base para decidir.
    [Fact]
    public void SupportsAmountCheck_WithFixedAmount_ShouldBeTrue()
    {
        Assert.True(LookupMother.BankSlip().SupportsAmountCheck);
    }

    // Valor editável pelo pagador não reprova nada — o check sai pulado, não aprovado.
    [Fact]
    public void SupportsAmountCheck_WhenValueIsOpen_ShouldBeFalse()
    {
        Assert.False(LookupMother.BankSlip(allowChangeValue: true).SupportsAmountCheck);
    }

    // Sem valor devolvido também não há o que comparar.
    [Fact]
    public void SupportsAmountCheck_WithoutAmount_ShouldBeFalse()
    {
        var snapshot = LookupSnapshot.Create(LookupParty.Of("Beneficiário"), LookupMother.ConsultedAt);

        Assert.False(snapshot.SupportsAmountCheck);
    }

    // A idade do retrato é o insumo da expiração de snapshot na aprovação.
    [Fact]
    public void AgeAt_ShouldMeasureTheDistanceFromTheConsultation()
    {
        var snapshot = LookupMother.BankSlip();

        Assert.Equal(TimeSpan.FromHours(3), snapshot.AgeAt(LookupMother.ConsultedAt.AddHours(3)));
    }

    // Retrato é imutável e comparado por valor: mesma consulta, mesmo retrato.
    [Fact]
    public void Equals_WithSameConsultedContent_ShouldBeTrue()
    {
        Assert.Equal(LookupMother.BankSlip(), LookupMother.BankSlip());
    }

    // Valor diferente é retrato diferente — nova consulta gera novo snapshot, nunca atualiza o anterior.
    [Fact]
    public void Equals_WithDifferentAmount_ShouldBeFalse()
    {
        Assert.NotEqual(LookupMother.BankSlip(), LookupMother.BankSlip(amount: LookupMother.Brl(999m)));
    }

    // Banco divergente entre consultas é retrato diferente — é o que sustenta a conferência
    // cruzada contra as posições 1–3 do código de barras.
    [Fact]
    public void Equals_WithDifferentBankCode_ShouldBeFalse()
    {
        Assert.NotEqual(LookupMother.BankSlip(), LookupMother.BankSlip(bankCode: new BankCode("237")));
    }
}
