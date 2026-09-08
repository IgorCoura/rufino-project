namespace BillPayment.UnitTests.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Services.Mothers;

/// <summary>
/// A trilha do boleto e a reversão de decisão terminal (ADR-018).
/// </summary>
/// <remarks>
/// O invariante que estes testes protegem é de acoplamento, não de estado: <strong>toda mudança
/// de status escreve uma linha</strong>. Quem acrescentar um caminho novo de mutação sem
/// registrar quebra o teste que varre o histórico do ciclo inteiro.
/// </remarks>
public class BillHistoryTests
{
    private static readonly UserId Approver = UserId.From(new Guid("0195a1f0-0000-7000-8000-00000000000a"));
    private static readonly UserId Reverter = UserId.From(new Guid("0195a1f0-0000-7000-8000-00000000000c"));
    private static readonly DateTime DecidedAt = new(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Capture_ShouldOpenTheHistoryWithNoPreviousStatus()
    {
        var bill = ValidationMother.BankSlipWithLookup();

        var entry = Assert.Single(bill.History);
        Assert.Equal(BillAction.Captured, entry.Action);

        // A única linha sem "antes" — e é isso que a torna reconhecível como a origem.
        Assert.Null(entry.FromStatus);
        Assert.Equal(BillStatus.Captured, entry.ToStatus);

        // Importar não é decidir: quem importa não autoriza pagamento nenhum.
        Assert.Null(entry.ActorUserId);
        Assert.Equal(BillHistoryEntry.SYSTEM_ACTOR_NAME, entry.ActorName);
    }

    [Fact]
    public void Deny_ShouldRecordWhoDecidedAndWhy()
    {
        var bill = ReadyForApproval();

        bill.Deny(Approver, "cobrança indevida", DecidedAt, denierName: "João");

        var entry = bill.History[^1];
        Assert.Equal(BillAction.Denied, entry.Action);
        Assert.Equal(Approver, entry.ActorUserId);
        Assert.Equal("João", entry.ActorName);
        Assert.Equal("cobrança indevida", entry.Note);
        Assert.Equal(BillStatus.AwaitingApproval, entry.FromStatus);
        Assert.Equal(BillStatus.Denied, entry.ToStatus);
    }

    // Pessoa sem nome no token NÃO vira "Sistema": mentiria sobre a natureza da ação. Fica o id,
    // que ao menos é rastreável até o provedor de identidade.
    [Fact]
    public void Deny_WithoutAnActorName_ShouldFallBackToTheUserIdNotToSystem()
    {
        var bill = ReadyForApproval();

        bill.Deny(Approver, "motivo", DecidedAt);

        var entry = bill.History[^1];
        Assert.Equal(Approver.Value.ToString(), entry.ActorName);
        Assert.NotEqual(BillHistoryEntry.SYSTEM_ACTOR_NAME, entry.ActorName);
    }

    [Fact]
    public void UndoDecision_OnADeniedBill_ShouldReturnToTheQueueAndEmitTheEvent()
    {
        var bill = ReadyForApproval();
        bill.Deny(Approver, "engano", DecidedAt);
        bill.PullDomainEvents();

        bill.UndoDecision(Reverter, "recusa feita por engano", DecidedAt, undoerName: "Ana");

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);

        var entry = bill.History[^1];
        Assert.Equal(BillAction.Reverted, entry.Action);
        Assert.Equal(Reverter, entry.ActorUserId);
        Assert.Equal("Ana", entry.ActorName);
        Assert.Equal(BillStatus.Denied, entry.FromStatus);

        var undone = Assert.IsType<BillDecisionUndoneDomainEvent>(Assert.Single(bill.PullDomainEvents()));
        Assert.Equal(Reverter, undone.UndoneBy);
    }

    [Fact]
    public void UndoDecision_OnACancelledBill_ShouldClearScheduleAndOrderLink()
    {
        var bill = ReadyForApproval();
        bill.Cancel(Approver, "importado por engano", DecidedAt);

        bill.UndoDecision(Reverter, "cancelamento indevido", DecidedAt);

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Null(bill.ScheduledFor);
        Assert.Null(bill.PaymentOrderId);
    }

    // A decisão ANTERIOR sobrevive à reversão: é história, e a próxima decisão grava a sua.
    [Fact]
    public void UndoDecision_ShouldKeepThePreviousApprovalRecordAsHistory()
    {
        var bill = ReadyForApproval();
        bill.Deny(Approver, "engano", DecidedAt);
        var denial = bill.Approval;

        bill.UndoDecision(Reverter, "revertido", DecidedAt);

        Assert.Same(denial, bill.Approval);
        Assert.Equal(ApprovalDecision.Denied, bill.Approval!.Decision);
    }

    [Theory]
    [InlineData("AwaitingApproval")]
    [InlineData("Approved")]
    public void UndoDecision_OnANonTerminalDecision_ShouldThrow_BLP_BIL38(string _)
    {
        var bill = ReadyForApproval();

        var ex = Assert.Throws<DomainException>(() => bill.UndoDecision(Reverter, "motivo", DecidedAt));

        Assert.Equal("BLP.BIL38", ex.Id);
        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
    }

    // Dinheiro que saiu não volta por decisão nossa — Paid é terminal de verdade.
    [Fact]
    public void UndoDecision_OnAPaidBill_ShouldThrow_BLP_BIL38()
    {
        var bill = ReadyForApproval();
        bill.Approve(Approver, null, ApprovalPolicy.Default(null), RiskLevel.ExtremeDanger, DecidedAt);
        bill.Schedule(Approver, new DateOnly(2026, 6, 24), ApprovalPolicy.Default(null), new DateOnly(2026, 6, 20), DecidedAt);
        bill.LinkPaymentOrder(Domain.PaymentOrders.PaymentOrderId.New(), new DateOnly(2026, 6, 24), DecidedAt);
        bill.MarkPaid(DecidedAt);

        var ex = Assert.Throws<DomainException>(() => bill.UndoDecision(Reverter, "motivo", DecidedAt));

        Assert.Equal("BLP.BIL38", ex.Id);
        Assert.Equal(BillStatus.Paid, bill.Status);
    }

    // 🔴 O DEFEITO QUE ESTES TESTES TRAVAM (achado em 2026-09-08, depois de a trilha entrar):
    // o cancelamento vindo do PROVEDOR e o pedido por uma PESSOA no nosso app passavam pelo mesmo
    // método e gravavam a mesma linha — autor "Sistema" nos dois. Quem auditasse não teria como
    // saber se o pagamento foi cancelado no painel do Asaas ou por alguém aqui dentro.
    [Fact]
    public void UnschedulePayment_FromTheProvider_ShouldSayItCameFromThereAndNameNoPerson()
    {
        var bill = ScheduledBill();

        bill.UnschedulePayment(BillActionOrigin.Provider, DecidedAt);

        var entry = bill.History[^1];
        Assert.Equal(BillActionOrigin.Provider, entry.Origin);
        Assert.Null(entry.ActorUserId);
        Assert.Equal(BillHistoryEntry.PROVIDER_ACTOR_NAME, entry.ActorName);
        Assert.Contains("NO PROVEDOR", entry.Note, StringComparison.Ordinal);
    }

    [Fact]
    public void UnschedulePayment_FromOurApp_ShouldNameThePersonWhoAskedForIt()
    {
        var bill = ScheduledBill();

        bill.UnschedulePayment(BillActionOrigin.User, DecidedAt, Approver, "Carla");

        var entry = bill.History[^1];
        Assert.Equal(BillActionOrigin.User, entry.Origin);
        Assert.Equal(Approver, entry.ActorUserId);
        Assert.Equal("Carla", entry.ActorName);
        Assert.DoesNotContain("NO PROVEDOR", entry.Note, StringComparison.Ordinal);
    }

    // Autor chegando junto de uma origem que não admite autor é descartado: atribuir a uma
    // pessoa um ato do provedor é pior que não ter autor nenhum.
    [Fact]
    public void UnschedulePayment_FromTheProvider_ShouldDiscardAnyActorHandedToIt()
    {
        var bill = ScheduledBill();

        bill.UnschedulePayment(BillActionOrigin.Provider, DecidedAt, Approver, "Carla");

        var entry = bill.History[^1];
        Assert.Null(entry.ActorUserId);
        Assert.Equal(BillHistoryEntry.PROVIDER_ACTOR_NAME, entry.ActorName);
    }

    // Os reflexos de execução são do provedor, não da nossa automação — a trilha diz de onde.
    [Fact]
    public void PaidAndFailed_ShouldBeAttributedToTheProvider()
    {
        var paid = ScheduledBill();
        paid.MarkPaid(DecidedAt);
        Assert.Equal(BillActionOrigin.Provider, paid.History[^1].Origin);

        var failed = ScheduledBill();
        failed.MarkFailed(DecidedAt);
        Assert.Equal(BillActionOrigin.Provider, failed.History[^1].Origin);
    }

    // Captura e validação são automação NOSSA: nem pessoa, nem provedor.
    [Fact]
    public void CaptureAndValidation_ShouldBeAttributedToOurOwnAutomation()
    {
        var bill = ReadyForApproval();

        Assert.Equal(BillActionOrigin.System, bill.History[0].Origin);
        Assert.Equal(BillAction.Validated, bill.History[1].Action);
        Assert.Equal(BillActionOrigin.System, bill.History[1].Origin);
    }

    // O invariante de acoplamento: nenhum caminho de mutação escapa da trilha. Quem acrescentar
    // uma transição nova sem registrar quebra este teste, que é exatamente o ponto.
    [Fact]
    public void TheWholeLifecycle_ShouldLeaveOneHistoryEntryPerStateChange()
    {
        var bill = ReadyForApproval();
        bill.Approve(Approver, null, ApprovalPolicy.Default(null), RiskLevel.ExtremeDanger, DecidedAt);
        bill.Schedule(Approver, new DateOnly(2026, 6, 24), ApprovalPolicy.Default(null), new DateOnly(2026, 6, 20), DecidedAt);
        bill.LinkPaymentOrder(Domain.PaymentOrders.PaymentOrderId.New(), new DateOnly(2026, 6, 24), DecidedAt);
        bill.UnschedulePayment(BillActionOrigin.Provider, DecidedAt);

        Assert.Equal(
            [
                BillAction.Captured,
                BillAction.Validated,
                BillAction.Approved,
                BillAction.Scheduled,
                BillAction.HandedToProvider,
                BillAction.Unscheduled,
            ],
            bill.History.Select(h => h.Action));
    }

    private static Bill ScheduledBill()
    {
        var bill = ReadyForApproval();
        bill.Approve(Approver, null, ApprovalPolicy.Default(null), RiskLevel.ExtremeDanger, DecidedAt);
        bill.Schedule(
            Approver, new DateOnly(2026, 6, 24), ApprovalPolicy.Default(null), new DateOnly(2026, 6, 20), DecidedAt);
        bill.LinkPaymentOrder(Domain.PaymentOrders.PaymentOrderId.New(), new DateOnly(2026, 6, 24), DecidedAt);
        return bill;
    }

    private static Bill ReadyForApproval()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.RecordChecks(
            [.. Enumeration.GetAll<CheckType>().Select(type => CheckResult.Passed(type))],
            DecidedAt);
        bill.PullDomainEvents();
        return bill;
    }
}
