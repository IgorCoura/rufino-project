namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Bills.Mothers;
using BillPayment.UnitTests.Instruments;
using BillPayment.UnitTests.Lookups.Mothers;
using BillPayment.UnitTests.Services.Mothers;

/// <summary>
/// A regra que se repetiu quebrada em quatro checks (2026-09-10): para DECIDIR contra o cadastro,
/// vale a consulta oficial do trilho que paga, com o outro trilho como reserva e o documento por
/// último. Confronto entre fontes — checks 4 e 12 — é outra coisa e continua confrontando.
/// </summary>
public class OfficialSourcePrecedenceTests
{
    private const string OtherTenantCnpj = "11444777000161";

    // REGRESSÃO: num documento híbrido cuja consulta do BOLETO falhou e cujo decode do Pix
    // respondeu, o check 4 saía Skipped — o ramo do Pix era inalcançável sempre que existisse um
    // código de barras. É onde arrecadação mais cai: lá o boleto não resolve e o Pix resolve.
    [Fact]
    public void Evaluate_LookupConsistency_WhenOnlyThePixRailAnswered_ShouldStillCompareIt()
    {
        var bill = BillMother.Capture([InstrumentSamples.UtilityBarcode(), InstrumentSamples.StaticPix()]);
        bill.AttachLookups(
            bankSlip: null,
            PixLookupResult.Resolved(LookupMother.PixDynamic(), ValidationMother.ConsultedAt),
            ValidationMother.OccurredAt);

        var result = Check(bill, CheckType.LookupConsistency);

        Assert.NotEqual(CheckOutcome.Skipped, result.Outcome);
        Assert.Equal(CheckReasons.LOOKUP_AMOUNT_MISMATCH, result.ReasonCode);
    }

    // Nenhum dos dois trilhos com retrato: aí sim não há o que comparar.
    [Fact]
    public void Evaluate_LookupConsistency_WithoutAnySnapshot_ShouldSkip()
    {
        var bill = BillMother.WithBothRails();

        var result = Check(bill, CheckType.LookupConsistency);

        Assert.Equal(CheckOutcome.Skipped, result.Outcome);
        Assert.Equal(CheckReasons.LOOKUP_UNAVAILABLE, result.ReasonCode);
    }

    // REGRESSÃO: o banco confrontado com o cadastro era sempre o do código de barras. Num
    // documento que liquida por Pix, esse banco não recebe nada — o dinheiro sai pelo PSP do
    // recebedor, e é ele que precisa estar entre os aceitos.
    [Fact]
    public void Evaluate_ReceivingBankMatch_OnThePixRail_ShouldUseTheBankThatWillActuallyReceive()
    {
        var payee = ValidationMother.RegisteredPayee(acceptedBank: "237");

        var result = Check(HybridPaidByPix(), CheckType.ReceivingBankMatch, payee);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
        Assert.Contains("237", result.Evidence, StringComparison.Ordinal);
    }

    // CONTRAPROVA: aceitar só o banco do código de barras não basta num documento pago por Pix —
    // o boleto continua reprovado, porque o destino real não está na lista.
    [Fact]
    public void Evaluate_ReceivingBankMatch_OnThePixRail_ShouldRejectWhenOnlyTheBarcodeBankIsAccepted()
    {
        var payee = ValidationMother.RegisteredPayee(acceptedBank: ValidationMother.BarcodeBankCode);

        var result = Check(HybridPaidByPix(), CheckType.ReceivingBankMatch, payee);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.BANK_NOT_ACCEPTED, result.ReasonCode);
    }

    // REGRESSÃO: o pagador que o decode do Pix devolve é fonte OFICIAL e estava atrás do CNPJ
    // inferido do PDF. Com o documento batendo com o cadastro, o check saía Passed e a
    // contradição oficial nunca era consultada.
    [Fact]
    public void Evaluate_PayerMatch_WhenTheOfficialPixPayerContradicts_ShouldBlockEvenIfTheDocumentAgrees()
    {
        var profile = ValidationMother.TenantProfile();
        var bill = BillMother.CaptureVerbatim(
            [InstrumentSamples.Barcode(), InstrumentSamples.DynamicPixQr()],
            BillMother.MailboxOrigin(),
            extractedPayer: PartyInfo.Of("RUFINO EMPREITEIRA LTDA", profile.PrimaryTaxId));

        bill.AttachLookups(
            bankSlip: null,
            PixLookupResult.Resolved(
                LookupMother.PixDynamic(payer: MaskedParty.Of("OUTRA EMPRESA", OtherTenantCnpj)),
                ValidationMother.ConsultedAt),
            ValidationMother.OccurredAt);

        var result = Check(bill, CheckType.PayerMatch, payerProfile: profile);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(CheckReasons.PAYER_MISMATCH, result.ReasonCode);
        Assert.True(result.IsBlockingFailure);
    }

    // REGRESSÃO: o check 10 refazia a precedência de vencimento à mão e PULAVA a linha digitável,
    // caindo direto na leitura por IA. Consulta oficial sem vencimento — 70% da arrecadação —
    // deixava o check inconclusivo sobre um documento cuja data está embutida e protegida por DV.
    [Fact]
    public void Evaluate_DueDateSanity_WhenTheLookupHasNoDueDate_ShouldUseTheBarcodeDateAndSaySo()
    {
        var withoutDueDate = LookupSnapshot.Create(
            LookupParty.From(LookupMother.BENEFICIARY_NAME, null, LookupMother.BENEFICIARY_CNPJ),
            ValidationMother.ConsultedAt,
            bankCode: new BankCode(ValidationMother.BarcodeBankCode),
            amount: ValidationMother.BarcodeAmount,
            originalAmount: ValidationMother.BarcodeAmount);

        var bill = ValidationMother.BankSlipWithLookup(withoutDueDate);

        var result = Check(bill, CheckType.DueDateSanity);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
        Assert.Same(DueDateSource.Barcode, bill.DueDateOrigin);
        Assert.Contains("código de barras", result.Evidence, StringComparison.Ordinal);
    }

    // O check 10 e a verificação 14 leem a MESMA data: a que o agregado consolidou.
    [Fact]
    public void Evaluate_DueDateSanity_ShouldReadTheSameDateTheAggregateConsolidated()
    {
        var bill = ValidationMother.BankSlipWithLookup();

        var result = Check(bill, CheckType.DueDateSanity);

        Assert.NotNull(bill.DueDate);
        Assert.Contains(bill.DueDate!.Value.ToString("yyyy-MM-dd"), result.Evidence, StringComparison.Ordinal);
        Assert.Same(DueDateSource.Official, bill.DueDateOrigin);
    }

    // POLÍTICA (decisão do usuário, 2026-09-10): consulta sem resposta leva o boleto a EXTREMO
    // PERIGO, não a Perigo. Sem ela ninguém confirmou o destino do dinheiro, e quem conseguisse
    // derrubar a consulta ganharia justamente a janela para aprovar o que não pode ser conferido.
    [Theory]
    [InlineData("connect_timeout", true, CheckReasons.LOOKUP_UNAVAILABLE)]
    [InlineData("unregistered_bank_slip", false, CheckReasons.LOOKUP_UNRESOLVED)]
    public void Evaluate_LookupAvailability_WhenTheLookupDoesNotAnswer_ShouldBeExtremeDanger(
        string providerReason, bool retryable, string expectedReason)
    {
        var bill = BillMother.Capture();
        var failure = retryable
            ? BillLookupResult.Unavailable(providerReason, null, ValidationMother.ConsultedAt)
            : BillLookupResult.Unresolved(providerReason, null, ValidationMother.ConsultedAt);

        var result = BillValidationService
            .Evaluate(ValidationMother.Context(bill, bankSlipLookup: failure))
            .Single(r => r.Type == CheckType.LookupAvailability);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Equal(expectedReason, result.ReasonCode);
        Assert.Same(RiskLevel.ExtremeDanger, result.RiskContribution);
    }

    // Tenant sem chave vinculada é a terceira ausência, e pede AÇÃO diferente: revalidar não
    // resolve nunca. Mesmo Extremo Perigo, motivo próprio — senão o aviso prometeria o que o
    // tempo não cumpre.
    [Fact]
    public void Evaluate_LookupAvailability_WithoutATenantKey_ShouldSayTheAccountIsNotLinked()
    {
        var bill = BillMother.Capture();
        var failure = BillLookupResult.Unavailable(
            LookupReasons.TENANT_KEY_NOT_CONFIGURED, null, ValidationMother.ConsultedAt);

        var result = BillValidationService
            .Evaluate(ValidationMother.Context(bill, bankSlipLookup: failure))
            .Single(r => r.Type == CheckType.LookupAvailability);

        Assert.Equal(CheckReasons.LOOKUP_NOT_CONFIGURED, result.ReasonCode);
        Assert.Same(RiskLevel.ExtremeDanger, result.RiskContribution);
    }

    // CONTRAPROVA: com a consulta respondendo, a verificação passa e não contribui risco nenhum.
    [Fact]
    public void Evaluate_LookupAvailability_WhenTheLookupAnswers_ShouldContributeNoRisk()
    {
        var result = Check(ValidationMother.BankSlipWithLookup(), CheckType.LookupAvailability);

        Assert.Equal(CheckOutcome.Passed, result.Outcome);
        Assert.Same(RiskLevel.Safe, result.RiskContribution);
    }

    /// <summary>Híbrido que liquida por Pix num banco (237) diferente do do código de barras (341).</summary>
    private static Bill HybridPaidByPix()
    {
        var bill = BillMother.Capture([InstrumentSamples.Barcode(), InstrumentSamples.DynamicPixQr()]);
        bill.AttachLookups(
            bankSlip: null,
            PixLookupResult.Resolved(
                LookupMother.PixDynamic(receiverIspb: "60746948"), ValidationMother.ConsultedAt),
            ValidationMother.OccurredAt);

        return bill;
    }

    private static CheckResult Check(
        Bill bill,
        CheckType type,
        Domain.Payees.Payee? payee = null,
        Domain.PayerProfiles.PayerProfile? payerProfile = null)
        => BillValidationService
            .Evaluate(ValidationMother.Context(bill, payee: payee, payerProfile: payerProfile))
            .Single(r => r.Type == type);
}
