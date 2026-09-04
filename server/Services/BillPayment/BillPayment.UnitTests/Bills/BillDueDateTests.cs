namespace BillPayment.UnitTests.Bills;

using BillPayment.Domain.Lookups;
using BillPayment.UnitTests.Bills.Mothers;
using BillPayment.UnitTests.Instruments;
using BillPayment.UnitTests.Lookups.Mothers;

/// <summary>
/// O vencimento consolidado do agregado — consulta oficial do trilho que paga primeiro, o outro
/// trilho como reserva, linha digitável por último. Nasceu da regressão de 2026-08-27: boletos
/// só-Pix listavam sem vencimento porque a projeção lia apenas a linha digitável.
/// </summary>
public class BillDueDateTests
{
    private static readonly DateTime EvaluatedAt = new(2026, 8, 5, 12, 30, 0, DateTimeKind.Utc);

    // Boleto capturado só com código de barras nasce com o vencimento embutido na linha digitável.
    [Fact]
    public void Capture_WithBarcodeOnly_ShouldExposeTheEmbeddedDueDate()
    {
        var instrument = InstrumentSamples.Barcode();
        var bill = BillMother.Capture([instrument]);

        Assert.NotNull(instrument.DigitableLine.DueDate);
        Assert.Equal(DateOnly.FromDateTime(instrument.DigitableLine.DueDate!.Value), bill.DueDate);
    }

    // QR Pix estático não carrega vencimento em lugar nenhum — o boleto nasce sem data.
    [Fact]
    public void Capture_WithStaticPixOnly_ShouldHaveNoDueDate()
    {
        var bill = BillMother.StaticPixOnly();

        Assert.Null(bill.DueDate);
    }

    // Teste de regressão: boleto só-Pix estava sem vencimento mesmo com o decode devolvendo a
    // data — anexar o retrato do decode preenche o vencimento consolidado.
    [Fact]
    public void AttachLookups_OnAPixOnlyBill_ShouldFillTheDueDateFromTheDecode()
    {
        var bill = BillMother.StaticPixOnly();

        bill.AttachLookups(
            null, PixLookupResult.Resolved(LookupMother.PixDynamic(), LookupMother.ConsultedAt), EvaluatedAt);

        Assert.Equal(LookupMother.DueDate, bill.DueDate);
    }

    // Documento híbrido paga por Pix (ADR-010) — com os dois retratos presentes, o vencimento
    // vem do decode Pix, mesma precedência de PayableAmount e Beneficiary.
    [Fact]
    public void AttachLookups_OnAPixRailBill_ShouldPreferThePixDueDate()
    {
        var bill = BillMother.WithBothRails();
        var bankSlipDueDate = new DateOnly(2026, 8, 15);

        bill.AttachLookups(
            BillLookupResult.Resolved(LookupMother.BankSlip(dueDate: bankSlipDueDate), LookupMother.ConsultedAt),
            PixLookupResult.Resolved(LookupMother.PixDynamic(), LookupMother.ConsultedAt),
            EvaluatedAt);

        Assert.Equal(LookupMother.DueDate, bill.DueDate);
    }

    // Boleto sem QR paga pelo código de barras — a consulta oficial da cobrança decide o
    // vencimento, sobrepondo a data embutida na linha.
    [Fact]
    public void AttachLookups_OnABoletoRailBill_ShouldPreferTheBankSlipDueDate()
    {
        var bill = BillMother.Capture([InstrumentSamples.Barcode()]);
        var officialDueDate = new DateOnly(2026, 8, 15);

        bill.AttachLookups(
            BillLookupResult.Resolved(LookupMother.BankSlip(dueDate: officialDueDate), LookupMother.ConsultedAt),
            null,
            EvaluatedAt);

        Assert.Equal(officialDueDate, bill.DueDate);
    }

    // Consulta indisponível não apaga o que a linha digitável já dizia — o vencimento embutido
    // permanece como reserva.
    [Fact]
    public void AttachLookups_WhenTheLookupIsUnavailable_ShouldKeepTheEmbeddedDueDate()
    {
        var instrument = InstrumentSamples.Barcode();
        var bill = BillMother.Capture([instrument]);

        bill.AttachLookups(
            BillLookupResult.Unavailable("timeout", null, LookupMother.ConsultedAt), null, EvaluatedAt);

        Assert.Equal(DateOnly.FromDateTime(instrument.DigitableLine.DueDate!.Value), bill.DueDate);
    }
}
