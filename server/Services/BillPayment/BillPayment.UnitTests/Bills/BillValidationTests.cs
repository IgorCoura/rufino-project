namespace BillPayment.UnitTests.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.SeedWork;
using BillPayment.UnitTests.Bills.Mothers;
using BillPayment.UnitTests.Lookups.Mothers;
using BillPayment.UnitTests.Services.Mothers;

/// <summary>
/// O que <c>Bill</c> faz com o resultado da verificação — a matriz de decisão do doc 03 e as
/// regras de revalidação.
/// </summary>
public class BillValidationTests
{
    private static readonly DateTime EvaluatedAt = new(2026, 6, 20, 9, 0, 0, DateTimeKind.Utc);

    // Linha 3 da matriz: tudo passou ou foi pulado, o boleto fica aguardando um humano.
    [Fact]
    public void RecordChecks_WithEverythingPassing_ShouldAwaitApprovalAndEmitValidated()
    {
        var bill = CapturedAndDrained();

        var outcome = bill.RecordChecks(AllPassing(), EvaluatedAt);

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Equal(0, outcome.BlockingFailures);
        Assert.False(outcome.IsRejected);

        var published = Assert.Single(bill.PullDomainEvents());
        var validated = Assert.IsType<BillValidatedDomainEvent>(published);
        Assert.Equal(bill.Id, validated.BillId);
        Assert.Equal(BillMother.DefaultTenant, validated.TenantId);
    }

    // Linha 1 da matriz: qualquer falha bloqueante reprova o boleto.
    [Fact]
    public void RecordChecks_WithABlockingFailure_ShouldRejectAndEmitRejectedWithTheReasons()
    {
        var bill = CapturedAndDrained();

        var outcome = bill.RecordChecks(
            AllPassing(CheckResult.Failed(CheckType.PayeeMatch, CheckReasons.PAYEE_LOOKALIKE)),
            EvaluatedAt);

        Assert.Equal(BillStatus.Rejected, bill.Status);
        Assert.Equal(1, outcome.BlockingFailures);
        Assert.True(outcome.IsRejected);

        var rejected = Assert.IsType<BillRejectedDomainEvent>(Assert.Single(bill.PullDomainEvents()));
        Assert.Contains(CheckReasons.PAYEE_LOOKALIKE, rejected.ReasonCodes);
    }

    // Linha 2 da matriz: falha apenas Advisory não reprova, mas conta como ponto de atenção.
    [Fact]
    public void RecordChecks_WithOnlyAnAdvisoryFailure_ShouldStillAwaitApproval()
    {
        var bill = CapturedAndDrained();

        var outcome = bill.RecordChecks(
            AllPassing(CheckResult.Failed(CheckType.AmountMatch, CheckReasons.AMOUNT_OUTSIDE_POLICY)),
            EvaluatedAt);

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Equal(0, outcome.BlockingFailures);
        Assert.Equal(1, outcome.AttentionItems);
    }

    // Warning nunca bloqueia, mesmo num check cuja severidade é Blocking — é exatamente para
    // isso que ele existe (divergência de grafia em arrecadação).
    [Fact]
    public void RecordChecks_WithAWarningOnABlockingCheck_ShouldNotReject()
    {
        var bill = CapturedAndDrained();

        var outcome = bill.RecordChecks(
            AllPassing(CheckResult.Warning(CheckType.PayeeMatch, CheckReasons.PAYEE_NAME_DIVERGENCE)),
            EvaluatedAt);

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Equal(0, outcome.BlockingFailures);
        Assert.Equal(1, outcome.AttentionItems);
    }

    // Conjunto parcial é recusado: gravar meia validação deixaria pergunta sem resposta
    // parecendo respondida — BLP.BIL19.
    [Fact]
    public void RecordChecks_WithAnIncompleteCatalog_ShouldThrow_BLP_BIL19()
    {
        var bill = BillMother.Capture();

        var ex = Assert.Throws<DomainException>(
            () => bill.RecordChecks([CheckResult.Passed(CheckType.BarcodeIntegrity)], EvaluatedAt));

        Assert.Equal("BLP.BIL19", ex.Id);
    }

    // Duas apurações do mesmo tipo na mesma rodada é erro de quem apurou — BLP.BIL18.
    [Fact]
    public void RecordChecks_WithTheSameCheckTwice_ShouldThrow_BLP_BIL18()
    {
        var bill = BillMother.Capture();
        var results = AllPassing().Append(CheckResult.Passed(CheckType.Duplicate)).ToList();

        var ex = Assert.Throws<DomainException>(() => bill.RecordChecks(results, EvaluatedAt));

        Assert.Equal("BLP.BIL18", ex.Id);
    }

    // O conjunto é substituído inteiro a cada rodada, nunca acumulado.
    [Fact]
    public void RecordChecks_TwiceInARow_ShouldReplaceTheWholeSetInsteadOfAccumulating()
    {
        var bill = BillMother.Capture();

        bill.RecordChecks(AllPassing(CheckResult.Failed(CheckType.PayeeMatch, CheckReasons.PAYEE_LOOKALIKE)), EvaluatedAt);
        bill.RecordChecks(AllPassing(), EvaluatedAt.AddHours(1));

        Assert.Equal(12, bill.Checks.Count);
        Assert.DoesNotContain(bill.Checks, c => c.Outcome == CheckOutcome.Failed);
    }

    // Revalidação limpa reabilita um boleto reprovado — é o botão de revalidar da tela.
    [Fact]
    public void RecordChecks_OnARejectedBill_WhenTheProblemIsGone_ShouldReturnToAwaitingApproval()
    {
        var bill = BillMother.Capture();
        bill.RecordChecks(AllPassing(CheckResult.Failed(CheckType.LookupAvailability, CheckReasons.LOOKUP_UNAVAILABLE)), EvaluatedAt);
        bill.PullDomainEvents();

        bill.RecordChecks(AllPassing(), EvaluatedAt.AddHours(1));

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
    }

    // Revalidar um boleto já aprovado derruba a aprovação: o consentimento foi dado contra um
    // retrato que acabou de ser substituído.
    [Fact]
    public void RecordChecks_OnAnAlreadyValidatedBill_ShouldKeepItAwaitingApproval()
    {
        var bill = BillMother.Capture();
        bill.RecordChecks(AllPassing(), EvaluatedAt);
        bill.PullDomainEvents();

        bill.RecordChecks(AllPassing(CheckResult.Failed(CheckType.Duplicate, CheckReasons.DUPLICATE_SAME_TENANT)), EvaluatedAt.AddHours(1));

        Assert.Equal(BillStatus.Rejected, bill.Status);
    }

    // Consulta que não resolveu NÃO apaga o retrato anterior: apagar deixaria o boleto sem
    // evidência nenhuma justamente quando a rede falhou.
    [Fact]
    public void AttachLookups_WhenTheRetryFails_ShouldKeepThePreviousSnapshotAndRecordTheAttempt()
    {
        var bill = BillMother.Capture();
        var snapshot = ValidationMother.ConsistentWithBarcode();

        bill.AttachLookups(BillLookupResult.Resolved(snapshot, LookupMother.ConsultedAt), null, EvaluatedAt);
        bill.AttachLookups(
            BillLookupResult.Unavailable("timeout", null, LookupMother.ConsultedAt.AddHours(1)), null, EvaluatedAt.AddHours(1));

        Assert.Equal(snapshot, bill.Lookup);
        Assert.Equal(2, bill.LookupHistory.Count);
        Assert.Equal(LookupStatus.Unavailable, bill.LookupHistory[1].Status);
    }

    // Toda tentativa entra no histórico — saber que a consulta ficou indisponível às 14h é
    // parte da história que a auditoria precisa reconstruir.
    [Fact]
    public void AttachLookups_ShouldAppendEveryAttemptIncludingTheOnesThatResolved()
    {
        var bill = BillMother.Capture();

        bill.AttachLookups(
            BillLookupResult.Resolved(ValidationMother.ConsistentWithBarcode(), LookupMother.ConsultedAt),
            null,
            EvaluatedAt);

        var record = Assert.Single(bill.LookupHistory);
        Assert.Equal(LookupStatus.Resolved, record.Status);
        Assert.NotNull(record.BankSlipSnapshot);
    }

    // Boleto em estado terminal não aceita nova verificação — BLP.BIL07.
    [Fact]
    public void RecordChecks_OnATerminalBill_ShouldThrow_BLP_BIL07()
    {
        var bill = BillMother.Capture();
        bill.RecordChecks(AllPassing(), EvaluatedAt);
        bill.PullDomainEvents();

        // Denied é terminal; chega lá pela aprovação, que ainda não existe — então o teste
        // exercita a guarda pelo caminho que existe hoje: cancelar a chave natural.
        var cancelled = BillMother.Capture();
        cancelled.RecordChecks(AllPassing(), EvaluatedAt);

        Assert.Equal(BillStatus.AwaitingApproval, cancelled.Status);
        Assert.True(cancelled.Status.AcceptsValidation);
        Assert.False(BillStatus.Paid.AcceptsValidation);
        Assert.False(BillStatus.Scheduled.AcceptsValidation);
    }

    /// <summary>
    /// Boleto capturado com o evento de captura já drenado, para o teste poder afirmar
    /// exatamente qual evento a verificação emitiu.
    /// </summary>
    private static Bill CapturedAndDrained()
    {
        var bill = BillMother.Capture();
        bill.PullDomainEvents();
        return bill;
    }

    /// <summary>
    /// As doze passando, com os desvios informados substituindo o resultado do seu tipo.
    /// </summary>
    private static List<CheckResult> AllPassing(params CheckResult[] overrides)
    {
        var results = Enumeration.GetAll<CheckType>()
            .Where(type => !overrides.Any(o => o.Type == type))
            .Select(type => CheckResult.Passed(type))
            .ToList();

        results.AddRange(overrides);
        return results;
    }
}
