namespace BillPayment.UnitTests.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.SeedWork;
using BillPayment.UnitTests.Bills.Mothers;
using BillPayment.UnitTests.Instruments;

public class BillTests
{
    // Capturar um boleto por código de barras nasce Captured, no trilho Boleto, como cobrança.
    [Fact]
    public void Capture_WithBarcodeOnly_ShouldStartCapturedOnTheBoletoRail()
    {
        var bill = BillMother.Capture();

        Assert.Same(BillStatus.Captured, bill.Status);
        Assert.Same(PaymentRail.Boleto, bill.Rail);
        Assert.Same(BillKind.BankSlip, bill.Kind);
        Assert.Single(bill.Instruments);
        Assert.Equal(BillMother.DefaultTenant, bill.TenantId);
        Assert.Equal(BillMother.DefaultOccurredAt, bill.CreatedAt);
    }

    // Havendo QR Pix, o trilho é Pix — ADR-010. A escolha é do agregado, não de quem chama.
    [Fact]
    public void Capture_WithBothInstruments_ShouldPreferThePixRail()
    {
        var bill = BillMother.WithBothRails();

        Assert.Same(PaymentRail.Pix, bill.Rail);
        Assert.Equal(2, bill.Instruments.Count);
    }

    // Só QR Pix também vai por Pix.
    [Fact]
    public void Capture_WithPixOnly_ShouldUseThePixRail()
    {
        var bill = BillMother.StaticPixOnly();

        Assert.Same(PaymentRail.Pix, bill.Rail);
    }

    // A natureza do documento vem do código de barras, nunca de quem importa.
    [Fact]
    public void Capture_WithUtilityBarcode_ShouldDeriveUtilityKind()
    {
        var bill = BillMother.Capture([InstrumentSamples.UtilityBarcode()]);

        Assert.Same(BillKind.Utility, bill.Kind);
    }

    // Sem código de barras não há campo de convênio: o documento é tratado como cobrança,
    // que é a leitura que mantém os checks mais exigentes ligados.
    [Fact]
    public void Capture_WithPixOnly_ShouldFallBackToBankSlipKind()
    {
        var bill = BillMother.StaticPixOnly();

        Assert.Same(BillKind.BankSlip, bill.Kind);
    }

    // Dois códigos de barras de naturezas diferentes são dois documentos — BLP.BIL15.
    [Fact]
    public void Capture_WithMixedBarcodeKinds_ShouldThrow_BLP_BIL15()
    {
        var ex = Assert.Throws<DomainException>(() => BillMother.Capture(
            [InstrumentSamples.Barcode(), InstrumentSamples.UtilityBarcode()]));

        Assert.Equal("BLP.BIL15", ex.Id);
    }

    // Dois códigos de barras de cobrança diferentes convivem — é o mesmo documento relido.
    [Fact]
    public void Capture_WithTwoBankSlipBarcodes_ShouldBeAccepted()
    {
        var bill = BillMother.Capture(
            [InstrumentSamples.Barcode(), InstrumentSamples.Barcode(InstrumentSamples.BankSlipLine033)]);

        Assert.Equal(2, bill.Instruments.Count);
        Assert.Same(BillKind.BankSlip, bill.Kind);
    }

    // Sem nenhuma forma de pagar não existe boleto — BLP.BIL08.
    [Fact]
    public void Capture_WithoutInstruments_ShouldThrow_BLP_BIL08()
    {
        var ex = Assert.Throws<DomainException>(() => BillMother.CaptureVerbatim([], BillMother.MailboxOrigin()));

        Assert.Equal("BLP.BIL08", ex.Id);
    }

    // O mesmo instrumento duas vezes indica extração repetida — BLP.BIL09.
    [Fact]
    public void Capture_WithTheSameInstrumentTwice_ShouldThrow_BLP_BIL09()
    {
        var ex = Assert.Throws<DomainException>(() => BillMother.Capture(
            [InstrumentSamples.Barcode(), InstrumentSamples.Barcode()]));

        Assert.Equal("BLP.BIL09", ex.Id);
    }

    // Origem ausente impede a captura — sem procedência não há auditoria depois.
    [Fact]
    public void Capture_WithoutOrigin_ShouldThrow_BLP_BIL10()
    {
        var ex = Assert.Throws<DomainException>(
            () => BillMother.CaptureVerbatim([InstrumentSamples.Barcode()], null!));

        Assert.Equal("BLP.BIL10", ex.Id);
    }

    // A chave de deduplicação é a do código de barras — a mais estável entre emissores.
    [Fact]
    public void Capture_WithBarcode_ShouldUseTheBarcodeAsDedupKey()
    {
        var bill = BillMother.Capture();

        Assert.NotNull(bill.DedupKey);
        Assert.StartsWith("bc:", bill.DedupKey, StringComparison.Ordinal);
    }

    // Com os dois trilhos, o código de barras ainda vence como chave de deduplicação.
    [Fact]
    public void Capture_WithBothInstruments_ShouldPreferTheBarcodeDedupKey()
    {
        var bill = BillMother.WithBothRails();

        Assert.StartsWith("bc:", bill.DedupKey, StringComparison.Ordinal);
    }

    // QR dinâmico nasce de uma cobrança específica e serve de chave.
    [Fact]
    public void Capture_WithDynamicPixOnly_ShouldUseThePixFingerprintAsDedupKey()
    {
        var bill = BillMother.Capture([InstrumentSamples.DynamicPixQr()]);

        Assert.StartsWith("pix:", bill.DedupKey, StringComparison.Ordinal);
    }

    // QR estático é reutilizável: deduplicar por ele bloquearia a conta do mês seguinte.
    // Sem chave, a unicidade global não se aplica e a defesa passa a ser (beneficiário, valor, vencimento).
    [Fact]
    public void Capture_WithStaticPixOnly_ShouldNotProduceADedupKey()
    {
        var bill = BillMother.StaticPixOnly();

        Assert.Null(bill.DedupKey);
    }

    // A captura anuncia o fato para disparar a consulta oficial e a validação.
    [Fact]
    public void Capture_ShouldEmitBillCapturedWithTheDerivedKindAndRail()
    {
        var bill = BillMother.WithBothRails();

        var captured = Assert.IsType<BillCapturedDomainEvent>(Assert.Single(bill.PullDomainEvents()));

        Assert.Equal(bill.Id, captured.BillId);
        Assert.Equal(BillMother.DefaultTenant, captured.TenantId);
        Assert.Equal("BankSlip", captured.Kind);
        Assert.Equal("Pix", captured.Rail);
        Assert.Equal(BillMother.DefaultOccurredAt, captured.OccurredAt);
        Assert.NotEqual(Guid.Empty, captured.EventId);
    }

    // O evento não carrega instrumento de pagamento: ele vai para o outbox e para o log.
    [Fact]
    public void BillCapturedEvent_ShouldNotCarryAnyPaymentInstrument()
    {
        var bill = BillMother.Capture();
        var captured = (BillCapturedDomainEvent)bill.PullDomainEvents().Single();

        var serialized = System.Text.Json.JsonSerializer.Serialize(captured);

        Assert.DoesNotContain(InstrumentSamples.BankSlipLine341, serialized, StringComparison.Ordinal);
    }

    // Upload manual não tem fonte de captura cadastrada e mesmo assim é origem válida.
    [Fact]
    public void Capture_WithManualUpload_ShouldBeAcceptedWithoutACaptureSource()
    {
        var bill = BillMother.Capture(origin: BillMother.ManualOrigin());

        Assert.Same(BillSourceKind.ManualUpload, bill.Origin.SourceKind);
        Assert.Null(bill.Origin.SourceId);
    }
}
