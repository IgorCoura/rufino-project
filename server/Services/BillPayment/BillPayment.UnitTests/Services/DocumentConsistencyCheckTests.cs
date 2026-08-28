namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Lookups.Mothers;
using BillPayment.UnitTests.Services.Mothers;

/// <summary>
/// O check 13 — o documento impresso (lido pela IA) contra a consulta oficial. A assimetria do
/// desenho: identidade contradita escala para Blocking; valor e vencimento divergem em aviso;
/// ausência nunca pesa (Fase E, 2026-08-27).
/// </summary>
public class DocumentConsistencyCheckTests
{
    private static readonly DateTimeOffset ReadAt = new(2026, 6, 20, 9, 0, 0, TimeSpan.Zero);

    // Sem leitura por IA não há o que comparar — Skipped, nunca falha.
    [Fact]
    public void Evaluate_WithoutAReading_ShouldSkipDocumentConsistency()
    {
        var bill = ValidationMother.BankSlipWithLookup();

        var check = EvaluateCheck(bill);

        Assert.Equal(CheckOutcome.Skipped, check.Outcome);
        Assert.Equal(CheckReasons.READING_NOT_AVAILABLE, check.ReasonCode);
    }

    // O CNPJ impresso no documento diverge do que a consulta oficial devolveu: é o vetor de
    // instrumento trocado sobre documento legítimo — Failed com escalada para Blocking (Perigo).
    [Fact]
    public void Evaluate_WhenThePrintedPayeeContradictsTheOfficialOne_ShouldFailBlocking()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.AttachReading(
            Reading(payeeTaxId: "45678901000256"), ReadAt.UtcDateTime);

        var check = EvaluateCheck(bill);

        Assert.Equal(CheckOutcome.Failed, check.Outcome);
        Assert.Equal(CheckReasons.DOCUMENT_PAYEE_MISMATCH, check.ReasonCode);
        Assert.True(check.IsBlockingFailure);
    }

    // O CNPJ impresso confere com o oficial — Passed, com a evidência dizendo o que foi conferido.
    [Fact]
    public void Evaluate_WhenThePrintedPayeeMatches_ShouldPass()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.AttachReading(
            Reading(payeeTaxId: LookupMother.BENEFICIARY_CNPJ), ReadAt.UtcDateTime);

        var check = EvaluateCheck(bill);

        Assert.Equal(CheckOutcome.Passed, check.Outcome);
        Assert.False(check.IsBlockingFailure);
    }

    // Valor impresso diferente do valor de face registrado é AVISO, não bloqueio — erro de OCR
    // ou de layout não pode rejeitar boleto legítimo.
    [Fact]
    public void Evaluate_WhenThePrintedAmountDiverges_ShouldWarnWithoutBlocking()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.AttachReading(
            Reading(payeeTaxId: LookupMother.BENEFICIARY_CNPJ, amount: 999.99m), ReadAt.UtcDateTime);

        var check = EvaluateCheck(bill);

        Assert.Equal(CheckOutcome.Warning, check.Outcome);
        Assert.Equal(CheckReasons.DOCUMENT_AMOUNT_DIVERGENCE, check.ReasonCode);
        Assert.False(check.IsBlockingFailure);
    }

    // A comparação de valor é contra o valor ORIGINAL: boleto vencido com encargos tem valor
    // atualizado maior que o impresso, e isso é legítimo — não gera nem aviso.
    [Fact]
    public void Evaluate_WhenOnlyTheUpdatedAmountGrewWithCharges_ShouldStillPass()
    {
        var withCharges = LookupSnapshot.Create(
            LookupParty.From(LookupMother.BENEFICIARY_NAME, null, LookupMother.BENEFICIARY_CNPJ),
            ValidationMother.ConsultedAt,
            bankCode: new BankCode(ValidationMother.BarcodeBankCode),
            amount: LookupMother.Brl(650.00m),
            originalAmount: LookupMother.Brl(615.07m),
            dueDate: new DateOnly(2026, 6, 25));
        var bill = ValidationMother.BankSlipWithLookup(withCharges);
        bill.AttachReading(
            Reading(payeeTaxId: LookupMother.BENEFICIARY_CNPJ, amount: 615.07m), ReadAt.UtcDateTime);

        var check = EvaluateCheck(bill);

        Assert.Equal(CheckOutcome.Passed, check.Outcome);
    }

    // Vencimento impresso longe do registrado (além da tolerância de 1 dia) é aviso.
    [Fact]
    public void Evaluate_WhenThePrintedDueDateDiverges_ShouldWarn()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.AttachReading(
            Reading(payeeTaxId: LookupMother.BENEFICIARY_CNPJ, dueDate: new DateOnly(2026, 8, 30)),
            ReadAt.UtcDateTime);

        var check = EvaluateCheck(bill);

        Assert.Equal(CheckOutcome.Warning, check.Outcome);
        Assert.Equal(CheckReasons.DOCUMENT_DUE_DATE_DIVERGENCE, check.ReasonCode);
    }

    // Leitura sem nenhum campo em comum com o oficial — Inconclusive: ausência não pesa (ADR-004).
    [Fact]
    public void Evaluate_WhenNothingIsComparable_ShouldBeInconclusive()
    {
        var bill = BillPayment.UnitTests.Bills.Mothers.BillMother.Capture();
        bill.AttachReading(Reading(description: "Conta de energia"), ReadAt.UtcDateTime);

        var check = EvaluateCheck(bill);

        Assert.Equal(CheckOutcome.Inconclusive, check.Outcome);
    }

    private static DocumentReading Reading(
        string? payeeTaxId = null,
        decimal? amount = null,
        DateOnly? dueDate = null,
        string? description = null)
        => DocumentReading.FromExtraction(
            ExtractedDocument.From(
                payeeTaxId: payeeTaxId,
                amount: amount,
                dueDate: dueDate,
                description: description),
            ReadAt);

    private static BillCheckLike EvaluateCheck(Domain.Bills.Bill bill)
    {
        var results = BillValidationService.Evaluate(ValidationMother.Context(bill));
        var result = results.Single(r => r.Type == CheckType.DocumentConsistency);

        return new BillCheckLike(result.Outcome, result.ReasonCode, result.IsBlockingFailure);
    }

    private sealed record BillCheckLike(CheckOutcome Outcome, string? ReasonCode, bool IsBlockingFailure);
}
