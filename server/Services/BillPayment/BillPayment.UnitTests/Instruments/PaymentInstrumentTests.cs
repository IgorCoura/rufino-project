namespace BillPayment.UnitTests.Instruments;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.SeedWork;

public class PaymentInstrumentTests
{
    // Instrumento de boleto expõe a linha digitável, o valor declarado e o trilho Boleto.
    [Fact]
    public void FromBarcode_ShouldExposeTheLineAmountAndRail()
    {
        var instrument = InstrumentSamples.Barcode();

        Assert.Same(PaymentInstrumentKind.Barcode, instrument.Kind);
        Assert.Same(PaymentRail.Boleto, instrument.Kind.Rail);
        Assert.Equal(615.07m, instrument.DeclaredAmount!.Amount);
        Assert.Equal("341", instrument.DigitableLine.BankCode.Value);
    }

    // Instrumento Pix expõe o payload, o valor declarado e o trilho Pix.
    [Fact]
    public void FromPixQr_ShouldExposeThePayloadAmountAndRail()
    {
        var instrument = InstrumentSamples.StaticPix();

        Assert.Same(PaymentInstrumentKind.PixQr, instrument.Kind);
        Assert.Same(PaymentRail.Pix, instrument.Kind.Rail);
        Assert.Equal(1500.00m, instrument.DeclaredAmount!.Amount);
        Assert.Equal("11222333000181", instrument.PixPayload.PixKey);
    }

    // A chave natural do boleto é o código de barras, prefixada para não colidir com a do Pix.
    [Fact]
    public void NaturalKey_ForBarcode_ShouldBeThePrefixedBarcode()
    {
        var instrument = InstrumentSamples.Barcode();

        Assert.Equal($"bc:{instrument.DigitableLine.Barcode}", instrument.NaturalKey);
    }

    // A chave natural do Pix é o hash do payload — o payload é longo demais para índice
    // e não deve ficar legível num log de conflito.
    [Fact]
    public void NaturalKey_ForPix_ShouldBeAHashAndNotThePayloadItself()
    {
        var instrument = InstrumentSamples.StaticPix();

        Assert.StartsWith("pix:", instrument.NaturalKey, StringComparison.Ordinal);
        Assert.DoesNotContain(InstrumentSamples.StaticPixWithAmount, instrument.NaturalKey, StringComparison.Ordinal);
        Assert.Equal(4 + 64, instrument.NaturalKey.Length);
    }

    // Boleto é sempre de uso único: a linha digitável nasce de um título específico.
    [Fact]
    public void IsSingleUse_ForBarcode_ShouldBeTrue()
    {
        Assert.True(InstrumentSamples.Barcode().IsSingleUse);
    }

    // QR dinâmico nasce de uma cobrança específica — serve de chave de deduplicação.
    [Fact]
    public void IsSingleUse_ForDynamicPix_ShouldBeTrue()
    {
        Assert.True(InstrumentSamples.DynamicPixQr().IsSingleUse);
    }

    // QR estático é reutilizável indefinidamente; usá-lo como chave bloquearia a conta
    // do mês seguinte por causa da do mês anterior.
    [Fact]
    public void IsSingleUse_ForStaticPix_ShouldBeFalse()
    {
        Assert.False(InstrumentSamples.StaticPix().IsSingleUse);
    }

    // Pedir a linha digitável de um instrumento Pix é erro de programação — BLP.INS03.
    [Fact]
    public void DigitableLine_OnPixInstrument_ShouldThrow_BLP_INS03()
    {
        var ex = Assert.Throws<DomainException>(() => InstrumentSamples.StaticPix().DigitableLine);

        Assert.Equal("BLP.INS03", ex.Id);
    }

    // Pedir o payload Pix de um instrumento de boleto é erro de programação — BLP.INS03.
    [Fact]
    public void PixPayload_OnBarcodeInstrument_ShouldThrow_BLP_INS03()
    {
        var ex = Assert.Throws<DomainException>(() => InstrumentSamples.Barcode().PixPayload);

        Assert.Equal("BLP.INS03", ex.Id);
    }

    // Instrumento sem conteúdo é recusado na construção.
    [Fact]
    public void FromBarcode_WithNull_ShouldThrow_BLP_INS01()
    {
        var ex = Assert.Throws<DomainException>(() => PaymentInstrument.FromBarcode(null!));

        Assert.Equal("BLP.INS01", ex.Id);
    }

    [Fact]
    public void FromPixQr_WithNull_ShouldThrow_BLP_INS02()
    {
        var ex = Assert.Throws<DomainException>(() => PaymentInstrument.FromPixQr(null!));

        Assert.Equal("BLP.INS02", ex.Id);
    }

    // Igualdade é pela chave natural: o mesmo instrumento lido duas vezes é o mesmo instrumento.
    [Fact]
    public void Equals_WithTheSameInstrumentReadTwice_ShouldBeEqual()
    {
        Assert.Equal(InstrumentSamples.Barcode(), InstrumentSamples.Barcode());
        Assert.NotEqual(InstrumentSamples.Barcode(), InstrumentSamples.Barcode(InstrumentSamples.BankSlipLine033));
    }

    // Boleto e Pix nunca colidem entre si, mesmo que os conteúdos fossem parecidos.
    [Fact]
    public void NaturalKey_AcrossKinds_ShouldNeverCollide()
    {
        Assert.NotEqual(InstrumentSamples.Barcode().NaturalKey, InstrumentSamples.StaticPix().NaturalKey);
    }
}
