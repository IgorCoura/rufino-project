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

    // REGRESSÃO (boleto do DAS, 2026-09-10): guia de imposto imprime UM par CNPJ/Razão Social,
    // o do contribuinte, e nenhum beneficiário. A leitura por IA atribuía esse CNPJ ao
    // beneficiário e o boleto de imposto nascia bloqueado como "instrumento trocado sobre
    // documento legítimo". Beneficiário igual ao pagador não descreve pagamento nenhum: a
    // leitura é descartada, e o check deixa de contradizer.
    [Fact]
    public void Evaluate_WhenTheReadPayeeIsTheTenantsOwnDocument_ShouldNotAccuseASwappedInstrument()
    {
        var profile = ValidationMother.TenantProfile();
        var bill = ValidationMother.BankSlipWithLookup();
        bill.AttachReading(Reading(payeeTaxId: profile.PrimaryTaxId.Value), ReadAt.UtcDateTime);

        var check = EvaluateCheck(bill, profile);

        Assert.Equal(CheckOutcome.Inconclusive, check.Outcome);
        Assert.Equal(CheckReasons.DOCUMENT_PAYEE_IS_THE_PAYER, check.ReasonCode);
        Assert.False(check.IsBlockingFailure);
    }

    // Descartado o beneficiário mal lido, o que sobrou da leitura continua sendo conferido — e
    // conferindo, o check passa. É o desfecho do DAS depois da correção.
    [Fact]
    public void Evaluate_WhenTheReadPayeeIsThePayerButTheRestMatches_ShouldPass()
    {
        var profile = ValidationMother.TenantProfile();
        var bill = ValidationMother.BankSlipWithLookup();
        bill.AttachReading(
            Reading(
                payeeTaxId: profile.PrimaryTaxId.Value,
                amount: ValidationMother.BarcodeAmount.Amount,
                dueDate: ValidationMother.BarcodeDueDate is { } d ? DateOnly.FromDateTime(d) : null),
            ReadAt.UtcDateTime);

        var check = EvaluateCheck(bill, profile);

        Assert.Equal(CheckOutcome.Passed, check.Outcome);
    }

    // CONTRAPROVA: o descarte alcança só o documento do PRÓPRIO pagador. Documento de terceiro
    // impresso como beneficiário continua sendo o vetor de fraude e continua bloqueando, mesmo
    // com o cadastro fiscal em mãos.
    [Fact]
    public void Evaluate_WhenTheReadPayeeIsAThirdParty_ShouldStillFailBlocking()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.AttachReading(Reading(payeeTaxId: "11444777000161"), ReadAt.UtcDateTime);

        var check = EvaluateCheck(bill, ValidationMother.TenantProfile());

        Assert.Equal(CheckOutcome.Failed, check.Outcome);
        Assert.Equal(CheckReasons.DOCUMENT_PAYEE_MISMATCH, check.ReasonCode);
        Assert.True(check.IsBlockingFailure);
    }

    // CONTRAPROVA: sem cadastro fiscal não há como saber que o documento lido é do próprio
    // pagador — e "não sei" nunca autoriza descartar evidência de fraude.
    [Fact]
    public void Evaluate_WithoutATaxProfile_ShouldNotDiscardTheReading()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.AttachReading(
            Reading(payeeTaxId: ValidationMother.TenantProfile().PrimaryTaxId.Value), ReadAt.UtcDateTime);

        var check = EvaluateCheck(bill);

        Assert.Equal(CheckOutcome.Failed, check.Outcome);
        Assert.True(check.IsBlockingFailure);
    }

    // POLÍTICA (decisão do usuário, 2026-09-10): no trilho PIX a identidade oficial é forte — o
    // decode devolve o CNPJ do recebedor —, então divergir dela é mais provável ser erro da
    // leitura por IA, que ninguém certifica, do que documento adulterado. Vira aviso com teto de
    // Atenção, com texto próprio: o alerta continua, deixa de decidir sozinho.
    [Fact]
    public void Evaluate_OnThePixRail_WhenThePrintedPayeeDiverges_ShouldWarnWithoutBlocking()
    {
        var bill = PaidByPixWithDivergentReading();

        var check = EvaluateCheck(bill);

        Assert.Equal(CheckOutcome.Warning, check.Outcome);
        Assert.Equal(CheckReasons.DOCUMENT_PAYEE_SUSPICION, check.ReasonCode);
        Assert.False(check.IsBlockingFailure);
    }

    // CONTRAPROVA: no trilho BOLETO a mesma divergência continua bloqueando. Lá a consulta devolve
    // menos — em arrecadação, nada de documento —, e o impresso é parte do que sustenta a
    // verificação.
    [Fact]
    public void Evaluate_OnTheBankSlipRail_WhenThePrintedPayeeDiverges_ShouldStillBlock()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.AttachReading(Reading(payeeTaxId: "11444777000161"), ReadAt.UtcDateTime);

        var check = EvaluateCheck(bill);

        Assert.Equal(CheckOutcome.Failed, check.Outcome);
        Assert.Equal(CheckReasons.DOCUMENT_PAYEE_MISMATCH, check.ReasonCode);
        Assert.True(check.IsBlockingFailure);
    }

    // TESTE-ÂNCORA (achado M2 da auditoria de 2026-09-03): o insumo deste check é a leitura por
    // IA, que funde o documento COM o corpo do e-mail. Documento fiscal que só existe no corpo foi
    // escrito por quem mandou a mensagem — bloquear por ele entregaria a essa pessoa o poder de
    // travar os pagamentos de quem recebe. Vira aviso, com o texto dizendo de onde veio.
    [Fact]
    public void Evaluate_WhenTheDivergentPayeeCameFromTheEmailBody_ShouldWarnWithoutBlocking()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.AttachReading(
            ReadingFromEmailBody("11444777000161"), ReadAt.UtcDateTime);

        var check = EvaluateCheck(bill);

        Assert.Equal(CheckOutcome.Warning, check.Outcome);
        Assert.Equal(CheckReasons.DOCUMENT_PAYEE_FROM_EMAIL_BODY, check.ReasonCode);
        Assert.False(check.IsBlockingFailure);
    }

    // CONTRAPROVA: o mesmo número, ausente do corpo do e-mail, continua bloqueando. A procedência
    // é apurada procurando os dígitos, não perguntando ao modelo — instrução injetada não a muda.
    [Fact]
    public void Evaluate_WhenTheEmailBodyDoesNotCarryThatTaxId_ShouldStillBlock()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.AttachReading(
            DocumentReading.FromExtraction(
                ExtractedDocument.From(payeeTaxId: "11444777000161"),
                ReadAt,
                untrustedText: "Segue em anexo o boleto do mês. Qualquer dúvida, respondemos."),
            ReadAt.UtcDateTime);

        var check = EvaluateCheck(bill);

        Assert.Equal(CheckOutcome.Failed, check.Outcome);
        Assert.True(check.IsBlockingFailure);
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

    /// <summary>
    /// Leitura cujo documento fiscal do beneficiário aparece no corpo do e-mail — a formatação
    /// diferente é de propósito: a apuração compara só os dígitos.
    /// </summary>
    private static DocumentReading ReadingFromEmailBody(string taxId)
        => DocumentReading.FromExtraction(
            ExtractedDocument.From(payeeTaxId: taxId),
            ReadAt,
            untrustedText: $"Prezado, o beneficiário desta cobrança é o CNPJ {FormatCnpj(taxId)}.");

    private static string FormatCnpj(string digits)
        => $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";

    /// <summary>Documento híbrido que liquida por Pix, com a leitura apontando outro beneficiário.</summary>
    private static Domain.Bills.Bill PaidByPixWithDivergentReading()
    {
        var bill = BillPayment.UnitTests.Bills.Mothers.BillMother.WithBothRails();

        bill.AttachLookups(
            BillLookupResult.Resolved(ValidationMother.ConsistentWithBarcode(), ValidationMother.ConsultedAt),
            PixLookupResult.Resolved(LookupMother.PixDynamic(), ValidationMother.ConsultedAt),
            ValidationMother.OccurredAt);

        bill.AttachReading(Reading(payeeTaxId: "11444777000161"), ReadAt.UtcDateTime);
        return bill;
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

    private static BillCheckLike EvaluateCheck(
        Domain.Bills.Bill bill, Domain.PayerProfiles.PayerProfile? payerProfile = null)
    {
        var results = BillValidationService.Evaluate(
            ValidationMother.Context(bill, payerProfile: payerProfile));
        var result = results.Single(r => r.Type == CheckType.DocumentConsistency);

        return new BillCheckLike(result.Outcome, result.ReasonCode, result.IsBlockingFailure);
    }

    private sealed record BillCheckLike(CheckOutcome Outcome, string? ReasonCode, bool IsBlockingFailure);
}
