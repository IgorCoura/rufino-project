namespace BillPayment.UnitTests.Lookups;

using BillPayment.Domain.Lookups;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Lookups.Mothers;

public class PixLookupSnapshotTests
{
    // O decode é a única fonte do CPF/CNPJ do recebedor — o BR Code carrega chave e nome, nunca documento.
    [Fact]
    public void Create_ForDynamicQr_ShouldCarryTheReceiverTaxIdAndInstitution()
    {
        var snapshot = LookupMother.PixDynamic();

        Assert.Equal(LookupMother.BENEFICIARY_CNPJ, snapshot.Receiver.TaxId!.Value);
        Assert.Equal("60701190", snapshot.ReceiverIspb);
        Assert.Same(TaxIdKind.CNPJ, snapshot.ReceiverKind);
        Assert.True(snapshot.IsDynamic);
    }

    // QR estático não carrega valor nem vencimento: os checks correspondentes saem pulados.
    [Fact]
    public void Create_ForStaticQr_ShouldHaveNoAmountOrDueDate()
    {
        var snapshot = LookupMother.PixStatic();

        Assert.False(snapshot.IsDynamic);
        Assert.Null(snapshot.PayableAmount);
        Assert.Null(snapshot.DueDate);
        Assert.False(snapshot.SupportsAmountCheck);
    }

    // O check de valor olha o total com encargos, não o nominal — é o total que será debitado.
    [Fact]
    public void PayableAmount_WithBothValues_ShouldPreferTheTotal()
    {
        var snapshot = LookupMother.PixDynamic();

        Assert.Equal(153.20m, snapshot.PayableAmount!.Amount);
        Assert.Equal(150.00m, snapshot.Amount!.Amount);
    }

    // Sem total, o nominal serve — é o que acontece quando não há encargo a aplicar.
    [Fact]
    public void PayableAmount_WithoutTotal_ShouldFallBackToTheNominalValue()
    {
        var snapshot = PixLookupSnapshot.Create(
            LookupParty.Of("Recebedor"),
            LookupMother.ConsultedAt,
            amount: LookupMother.Brl(42m));

        Assert.Equal(42m, snapshot.PayableAmount!.Amount);
    }

    // Porteira anterior a tudo: QR que o provedor já sabe que não paga não deve consumir
    // verificação nem chegar à tela de aprovação.
    [Fact]
    public void Create_WhenProviderRefusesTheQr_ShouldRecordTheReason()
    {
        var snapshot = PixLookupSnapshot.Create(
            LookupParty.Of("Recebedor"),
            LookupMother.ConsultedAt,
            canBePaid: false,
            cannotBePaidReason: "QR_CODE_EXPIRED");

        Assert.False(snapshot.CanBePaid);
        Assert.Equal("QR_CODE_EXPIRED", snapshot.CannotBePaidReason);
    }

    // O QR vence: depois do prazo, pagar deixa de ser possível mesmo com tudo mais em ordem.
    [Fact]
    public void IsExpiredAt_AfterTheExpirationDate_ShouldBeTrue()
    {
        var expiresAt = LookupMother.ConsultedAt.AddHours(2);
        var snapshot = PixLookupSnapshot.Create(
            LookupParty.Of("Recebedor"),
            LookupMother.ConsultedAt,
            expirationDate: expiresAt);

        Assert.False(snapshot.IsExpiredAt(expiresAt.AddMinutes(-1)));
        Assert.True(snapshot.IsExpiredAt(expiresAt.AddMinutes(1)));
    }

    // Sem prazo declarado, o QR não expira por si — o estático é justamente esse caso.
    [Fact]
    public void IsExpiredAt_WithoutAnExpirationDate_ShouldBeFalse()
    {
        Assert.False(LookupMother.PixStatic().IsExpiredAt(LookupMother.ConsultedAt.AddYears(1)));
    }

    // Valor aberto pula o check de valor em vez de reprová-lo.
    [Fact]
    public void SupportsAmountCheck_WhenTheQrAcceptsAnotherValue_ShouldBeFalse()
    {
        var snapshot = PixLookupSnapshot.Create(
            LookupParty.Of("Recebedor"),
            LookupMother.ConsultedAt,
            totalAmount: LookupMother.Brl(10m),
            canBePaidWithDifferentValue: true);

        Assert.False(snapshot.SupportsAmountCheck);
    }

    // O pagador mascarado é guardado para poder contradizer — nunca para confirmar.
    [Fact]
    public void Create_WithMaskedPayer_ShouldKeepItForContradiction()
    {
        var snapshot = LookupMother.PixDynamic(payer: MaskedParty.Of("F*** J***", "***.982.247-**"));

        Assert.NotNull(snapshot.Payer);
        Assert.False(snapshot.Payer!.IsCompatibleWith(TaxId.Parse("11144477735")));
    }

    // Sem recebedor não há retrato — BLP.LKP02.
    [Fact]
    public void Create_WithoutReceiver_ShouldThrow_BLP_LKP02()
    {
        var ex = Assert.Throws<DomainException>(
            () => PixLookupSnapshot.Create(null!, LookupMother.ConsultedAt));

        Assert.Equal("BLP.LKP02", ex.Id);
    }

    // Retrato do Pix também é imutável e comparado por valor.
    [Fact]
    public void Equals_WithSameConsultedContent_ShouldBeTrue()
    {
        Assert.Equal(LookupMother.PixDynamic(), LookupMother.PixDynamic());
    }

    // Total diferente é retrato diferente.
    [Fact]
    public void Equals_WithDifferentTotal_ShouldBeFalse()
    {
        Assert.NotEqual(LookupMother.PixDynamic(), LookupMother.PixDynamic(totalAmount: LookupMother.Brl(1m)));
    }
}
