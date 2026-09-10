namespace BillPayment.UnitTests.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
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
    private static readonly UserId Approver = UserId.From(new Guid("0195a1f0-0000-7000-8000-00000000000a"));
    private static readonly DateOnly Today = new(2026, 6, 20);
    private static readonly DateOnly ScheduleFor = new(2026, 6, 24);

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
    public void RecordChecks_WithABlockingFailure_ShouldClassifyAsDangerAndAwaitTheHuman()
    {
        var bill = CapturedAndDrained();

        var outcome = bill.RecordChecks(
            AllPassing(CheckResult.Failed(CheckType.PayeeMatch, CheckReasons.PAYEE_LOOKALIKE)),
            EvaluatedAt);

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Same(RiskLevel.Danger, bill.Risk);
        Assert.Equal(1, outcome.BlockingFailures);

        Assert.IsType<BillValidatedDomainEvent>(Assert.Single(bill.PullDomainEvents()));
    }

    // Linha nova da matriz (2026-08-31): falha CRÍTICA — declaração explícita do tenant, como
    // blacklist — vence a bloqueante comum e classifica como Extremo Perigo.
    [Fact]
    public void RecordChecks_WithACriticalFailure_ShouldClassifyAsExtremeDanger()
    {
        var bill = CapturedAndDrained();

        bill.RecordChecks(
            AllPassing(
                CheckResult.Failed(
                    CheckType.PayeeMatch, CheckReasons.PAYEE_BLACKLISTED, severity: CheckSeverity.Critical),
                CheckResult.Failed(CheckType.Duplicate, CheckReasons.DUPLICATE_SAME_TENANT)),
            EvaluatedAt);

        Assert.Same(RiskLevel.ExtremeDanger, bill.Risk);
    }

    // Linha 2 da matriz, reescrita pelo ADR-020 (2026-09-08): falha apenas Advisory continua
    // sem reprovar, mas passou a classificar como PERIGO. Antes disso era Atenção — a mudança
    // é o coração do endurecimento, e é aqui que ela fica afirmada.
    [Fact]
    public void RecordChecks_WithOnlyAnAdvisoryFailure_ShouldClassifyAsDanger()
    {
        var bill = CapturedAndDrained();

        var outcome = bill.RecordChecks(
            AllPassing(CheckResult.Failed(CheckType.AmountMatch, CheckReasons.AMOUNT_OUTSIDE_POLICY)),
            EvaluatedAt);

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Same(RiskLevel.Danger, bill.Risk);
        Assert.Same(RiskLevel.Danger, outcome.Risk);
        Assert.Equal(0, outcome.BlockingFailures);
        Assert.Equal(1, outcome.AttentionItems);
    }

    // Inconclusivo também pesa como Perigo desde o ADR-020 — é o desfecho mais comum do
    // catálogo, e promovê-lo foi decisão explícita do usuário.
    [Fact]
    public void RecordChecks_WithAnInconclusiveOutcome_ShouldClassifyAsDanger()
    {
        var bill = CapturedAndDrained();

        bill.RecordChecks(
            AllPassing(CheckResult.Inconclusive(CheckType.OriginTrust, CheckReasons.ORIGIN_UNKNOWN)),
            EvaluatedAt);

        Assert.Same(RiskLevel.Danger, bill.Risk);
    }

    // Warning nunca bloqueia, mesmo num check cuja severidade é Blocking — é exatamente para
    // isso que ele existe. Mas ele CLASSIFICA como Perigo, salvo o teto do Notice.
    [Fact]
    public void RecordChecks_WithAWarningOnABlockingCheck_ShouldNotRejectButShouldClassifyAsDanger()
    {
        var bill = CapturedAndDrained();

        var outcome = bill.RecordChecks(
            AllPassing(CheckResult.Warning(CheckType.PayeeMatch, CheckReasons.PAYEE_NAME_DIVERGENCE)),
            EvaluatedAt);

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Same(RiskLevel.Danger, bill.Risk);
        Assert.Equal(0, outcome.BlockingFailures);
        Assert.Equal(1, outcome.AttentionItems);
    }

    // O TETO DO NOTICE, que é o carve-out do ADR-020: os três assuntos marcados Notice —
    // expectativa, prazo e nome do beneficiário — param em Atenção, qualquer que seja o
    // resultado. Sem isto, a promoção acima levaria tudo junto.
    [Theory]
    [InlineData(nameof(CheckOutcome.Warning))]
    [InlineData(nameof(CheckOutcome.Inconclusive))]
    [InlineData(nameof(CheckOutcome.Failed))]
    public void RecordChecks_WithOnlyANoticeOutcome_ShouldStopAtAttention(string outcomeName)
    {
        var bill = CapturedAndDrained();
        var outcome = Enumeration.FromDisplayName<CheckOutcome>(outcomeName);

        var noticed = outcome == CheckOutcome.Failed
            ? CheckResult.Failed(
                CheckType.DueDateSanity, CheckReasons.OVERDUE, severity: CheckSeverity.Notice)
            : outcome == CheckOutcome.Warning
                ? CheckResult.Warning(
                    CheckType.PayeeMatch, CheckReasons.PAYEE_NAME_DIVERGENCE, severity: CheckSeverity.Notice)
                : CheckResult.Inconclusive(
                    CheckType.ExpectationMatch, CheckReasons.EXPECTATION_NOT_REGISTERED);

        var result = bill.RecordChecks(AllPassing(noticed), EvaluatedAt);

        Assert.Same(RiskLevel.Attention, bill.Risk);
        Assert.Same(RiskLevel.Attention, result.Risk);
    }

    // O pior vence: o Notice não rebaixa nada — um boleto com prazo ruim E conferência
    // incompleta continua sendo Perigo.
    [Fact]
    public void RecordChecks_WithANoticeAndADangerTogether_ShouldTakeTheWorst()
    {
        var bill = CapturedAndDrained();

        bill.RecordChecks(
            AllPassing(
                CheckResult.Failed(
                    CheckType.DueDateSanity, CheckReasons.OVERDUE, severity: CheckSeverity.Notice),
                CheckResult.Inconclusive(CheckType.OriginTrust, CheckReasons.ORIGIN_UNKNOWN)),
            EvaluatedAt);

        Assert.Same(RiskLevel.Danger, bill.Risk);
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

        Assert.Equal(Enumeration.GetAll<CheckType>().Count(), bill.Checks.Count);
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
        Assert.Same(RiskLevel.Safe, bill.Risk);

        bill.RecordChecks(AllPassing(CheckResult.Failed(CheckType.Duplicate, CheckReasons.DUPLICATE_SAME_TENANT)), EvaluatedAt.AddHours(1));

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Same(RiskLevel.Danger, bill.Risk);
    }

    // 2026-09-10: revalidar um APROVADO sem nada mudar mantém a aprovação. Antes derrubava
    // incondicionalmente, e isso fechava um laço sem saída — o agendamento exige retrato fresco,
    // revalidar era o único jeito de renová-lo, e revalidar custava a aprovação.
    [Fact]
    public void RecordChecks_OnAnApprovedBill_WhenNothingChanged_ShouldKeepTheApproval()
    {
        var bill = Approved();

        var outcome = bill.RecordChecks(AllPassing(), EvaluatedAt.AddHours(13));

        Assert.Equal(BillStatus.Approved, bill.Status);
        Assert.True(outcome.ApprovalPreserved);
        Assert.Equal(BillStatus.Approved, outcome.Status);

        // A aprovação vigente continua sendo a mesma pessoa, no mesmo instante.
        Assert.Equal(Approver, bill.Approval!.DecidedBy);
        Assert.Equal(ApprovalDecision.Approved, bill.Approval.Decision);
    }

    // E a trilha registra a revalidação que NÃO mudou o status: meses depois alguém vai perguntar
    // por que a aprovação sobreviveu a uma consulta nova.
    [Fact]
    public void RecordChecks_WhenTheApprovalSurvives_ShouldStillRecordTheValidationInTheTrail()
    {
        var bill = Approved();
        var before = bill.History.Count;

        bill.RecordChecks(AllPassing(), EvaluatedAt.AddHours(13));

        var entry = bill.History[^1];
        Assert.Equal(before + 1, bill.History.Count);
        Assert.Equal(BillAction.Validated, entry.Action);
        Assert.Equal(BillStatus.Approved, entry.FromStatus);
        Assert.Equal(BillStatus.Approved, entry.ToStatus);
        Assert.Contains("aprovação mantida", entry.Note, StringComparison.Ordinal);
    }

    // Desfecho, severidade e motivo: cada um deles, sozinho, é mudança — e mudança derruba.
    [Theory]
    [MemberData(nameof(ChangedChecks))]
    public void RecordChecks_OnAnApprovedBill_WhenACheckChanged_ShouldFallBackToAwaitingApproval(
        CheckResult changed)
    {
        var bill = Approved();

        var outcome = bill.RecordChecks(AllPassing(changed), EvaluatedAt.AddHours(13));

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.False(outcome.ApprovalPreserved);
    }

    public static TheoryData<CheckResult> ChangedChecks() => new()
    {
        // Desfecho diferente.
        CheckResult.Failed(CheckType.PayeeMatch, CheckReasons.PAYEE_LOOKALIKE),
        // Mesmo desfecho, severidade diferente — o caso do DocumentConsistency que escala para
        // Blocking quando a identidade do beneficiário divergiu.
        CheckResult.Failed(
            CheckType.DocumentConsistency,
            CheckReasons.DOCUMENT_PAYEE_MISMATCH,
            severity: CheckSeverity.Blocking),
        // Mesmo desfecho e severidade, MOTIVO diferente: a razão mudou, e é ela que a tela traduz
        // para o aprovador.
        CheckResult.Passed(
            CheckType.ExpectationMatch, reasonCode: CheckReasons.EXPECTATION_NOT_REGISTERED),
    };

    // A evidência fica FORA da comparação de propósito: é texto com valores e datas dentro, muda a
    // cada consulta, e incluí-la faria a preservação nunca acontecer.
    [Fact]
    public void RecordChecks_OnAnApprovedBill_WhenOnlyTheEvidenceTextChanged_ShouldKeepTheApproval()
    {
        var bill = Approved();

        var withNewEvidence = AllPassing(
            CheckResult.Passed(CheckType.LookupAvailability, evidence: "consultado às 22h de 20/06"));

        var outcome = bill.RecordChecks(withNewEvidence, EvaluatedAt.AddHours(13));

        Assert.Equal(BillStatus.Approved, bill.Status);
        Assert.True(outcome.ApprovalPreserved);
    }

    // A outra metade da regra, e a que protege dinheiro: as catorze podem sair IDÊNTICAS enquanto
    // o valor a pagar sobe — é o que acontece todo dia num boleto vencido, porque AmountMatch
    // compara contra a política do beneficiário, não contra o número que o aprovador viu.
    [Fact]
    public void RecordChecks_OnAnApprovedBill_WhenThePayableAmountChanged_ShouldFallBackToAwaitingApproval()
    {
        var bill = Approved();

        // Mesma consulta, valor maior: o encargo do atraso entrou.
        bill.AttachLookups(
            BillLookupResult.Resolved(
                ValidationMother.ConsistentWithBarcode(amount: ValidationMother.BarcodeAmount.Add(LookupMother.Brl(40m))),
                ValidationMother.ConsultedAt.AddHours(13)),
            null,
            EvaluatedAt.AddHours(13));

        var outcome = bill.RecordChecks(AllPassing(), EvaluatedAt.AddHours(13));

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.False(outcome.ApprovalPreserved);
    }

    // Boleto aprovado ANTES de o valor consentido passar a ser gravado cai uma vez para a fila de
    // decisão: sem saber contra o que a pessoa consentiu, manter a aprovação seria presumir.
    [Fact]
    public void RecordChecks_OnALegacyApprovalWithoutTheAmount_ShouldFallBackToAwaitingApproval()
    {
        var bill = Approved();
        LegacyApprovalWithoutAmount(bill);

        var outcome = bill.RecordChecks(AllPassing(), EvaluatedAt.AddHours(13));

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.False(outcome.ApprovalPreserved);
    }

    // Boleto com data escolhida está a caminho do provedor. Antes desta guarda a revalidação o
    // levava a AwaitingApproval SEM limpar a data — aguardando aprovação com ordem em voo.
    [Fact]
    public void RecordChecks_OnAScheduledBill_ShouldThrow_BLP_BIL41()
    {
        var bill = Approved();
        bill.Schedule(
            Approver, ScheduleFor, ApprovalPolicy.Default(null), Today, SameDayScheduling.Allow(), EvaluatedAt);

        var ex = Assert.Throws<DomainException>(() => bill.RecordChecks(AllPassing(), EvaluatedAt.AddHours(1)));

        Assert.Equal("BLP.BIL41", ex.Id);
        Assert.Equal(BillStatus.Approved, bill.Status);
        Assert.Equal(ScheduleFor, bill.ScheduledFor);
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

    /// <summary>Boleto com retrato oficial, verificado e aprovado, com os eventos drenados.</summary>
    private static Bill Approved()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.RecordChecks(AllPassing(), EvaluatedAt);
        bill.Approve(Approver, null, ApprovalPolicy.Default(null), RiskLevel.ExtremeDanger, EvaluatedAt);
        bill.PullDomainEvents();
        return bill;
    }

    /// <summary>
    /// Reescreve a decisão vigente sem o valor consentido, como as aprovações gravadas antes de
    /// <c>AmountAtDecision</c> existir.
    /// </summary>
    /// <remarks>
    /// O caminho é reflexão de propósito: não existe — e não deve existir — API de domínio para
    /// fabricar uma aprovação incompleta. O que o teste precisa reproduzir é uma LINHA de banco
    /// anterior à migração, e essa linha é o único jeito honesto de descrevê-la.
    /// </remarks>
    private static void LegacyApprovalWithoutAmount(Bill bill)
    {
        var legacy = ApprovalRecord.Approve(Approver, EvaluatedAt, null, RiskLevel.Safe);

        typeof(Bill)
            .GetProperty(nameof(Bill.Approval))!
            .SetValue(bill, legacy);
    }

    /// <summary>
    /// As catorze passando, com os desvios informados substituindo o resultado do seu tipo.
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
