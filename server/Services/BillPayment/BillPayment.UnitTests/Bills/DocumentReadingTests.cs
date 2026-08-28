namespace BillPayment.UnitTests.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Extraction;
using BillPayment.UnitTests.Bills.Mothers;
using BillPayment.UnitTests.Instruments;

/// <summary>
/// O retrato da leitura por IA — campos tipados, DV decidindo documento, competência normalizada.
/// </summary>
public class DocumentReadingTests
{
    private static readonly DateTimeOffset ReadAt = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    // Documento fiscal só entra com DV válido: número ilegível vira ausência, nunca chute (ADR-011).
    [Fact]
    public void FromExtraction_WithAnInvalidTaxId_ShouldDropItToAbsence()
    {
        var extracted = ExtractedDocument.From(
            payerTaxId: "11222333000199", payeeTaxId: "45678901000175");

        var reading = DocumentReading.FromExtraction(extracted, ReadAt);

        Assert.Null(reading.PayerTaxId);
        Assert.NotNull(reading.PayeeTaxId);
        Assert.Equal("45678901000175", reading.PayeeTaxId!.Value);
    }

    // A competência declarada é normalizada nos formatos reais de fatura; fora deles, ausência.
    [Theory]
    [InlineData("07/2026", 2026, 7)]
    [InlineData("7/2026", 2026, 7)]
    [InlineData("2026-07", 2026, 7)]
    [InlineData("julho/2026", 2026, 7)]
    [InlineData("Julho de 2026", 2026, 7)]
    [InlineData("MARÇO/2026", 2026, 3)]
    public void TryParseCompetence_WithARealWorldFormat_ShouldNormalize(string text, int year, int month)
    {
        var competence = DocumentReading.TryParseCompetence(text);

        Assert.NotNull(competence);
        Assert.Equal(year, competence!.Year);
        Assert.Equal(month, competence.Month);
    }

    // Texto que não descreve competência nenhuma vira ausência — nunca uma data inventada.
    [Theory]
    [InlineData("13/2026")]
    [InlineData("proximo mes")]
    [InlineData("2026")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParseCompetence_WithGarbage_ShouldReturnNull(string? text)
        => Assert.Null(DocumentReading.TryParseCompetence(text));

    // Leitura sem conteúdo nenhum se declara vazia — é o que impede gravar retrato oco na Bill.
    [Fact]
    public void FromExtraction_WithAnEmptyExtraction_ShouldHaveNoContent()
    {
        var reading = DocumentReading.FromExtraction(ExtractedDocument.Empty, ReadAt);

        Assert.False(reading.HasContent);
    }

    // A leitura por IA é a ÚLTIMA reserva do vencimento consolidado: QR estático sem consulta
    // ganha a data lida do documento — decisão de 2026-08-27, que alimenta o agendamento.
    [Fact]
    public void AttachReading_OnAStaticPixBillWithoutLookup_ShouldFillTheDueDateFromTheReading()
    {
        var bill = BillMother.StaticPixOnly();
        var reading = DocumentReading.FromExtraction(
            ExtractedDocument.From(dueDate: new DateOnly(2026, 9, 10)), ReadAt);

        bill.AttachReading(reading, ReadAt.UtcDateTime);

        Assert.Equal(new DateOnly(2026, 9, 10), bill.DueDate);
    }

    // A data embutida na linha digitável (protegida por DV) vence a data lida pelo modelo.
    [Fact]
    public void AttachReading_OnABarcodeBill_ShouldKeepTheEmbeddedDueDate()
    {
        var instrument = InstrumentSamples.Barcode();
        var bill = BillMother.Capture([instrument]);
        var reading = DocumentReading.FromExtraction(
            ExtractedDocument.From(dueDate: new DateOnly(2026, 12, 31)), ReadAt);

        bill.AttachReading(reading, ReadAt.UtcDateTime);

        Assert.Equal(DateOnly.FromDateTime(instrument.DigitableLine.DueDate!.Value), bill.DueDate);
    }
}
