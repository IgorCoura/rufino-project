namespace BillPayment.UnitTests.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Bills.Mothers;
using BillPayment.UnitTests.Services.Mothers;

/// <summary>
/// O boleto como ESPELHO da ordem de pagamento (ADR-002): os reflexos Scheduled/Paid/Failed/
/// Cancelled, a reabertura, e a guarda de vencido do ADR-017 na aprovação.
/// </summary>
public class BillPaymentMirrorTests
{
    private static readonly UserId Approver = UserId.From(new Guid("0195a1f0-0000-7000-8000-00000000000a"));
    private static readonly DateTime DecidedAt = new(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 6, 20);
    private static readonly DateOnly ScheduleFor = new(2026, 6, 24);
    private static readonly PaymentOrderId OrderId =
        PaymentOrderId.From(new Guid("0195a1f0-0000-7000-8000-0000000000e1"));

    // O provedor aceitou: o boleto agenda, guarda o vínculo com a ordem e passa a mostrar a
    // data EFETIVA — a pedida vive na trilha e na ordem.
    [Fact]
    public void LinkPaymentOrder_OnAnApprovedBill_ShouldScheduleWithTheEffectiveDate()
    {
        var bill = Approved();
        var effective = new DateOnly(2026, 6, 25);

        bill.LinkPaymentOrder(OrderId, effective, DecidedAt);

        Assert.Equal(BillStatus.Scheduled, bill.Status);
        Assert.Equal(OrderId, bill.PaymentOrderId);
        Assert.Equal(effective, bill.ScheduledFor);
    }

    // Refletir agendamento num boleto que não está aprovado é evento fora de ordem — BLP.BIL34.
    [Fact]
    public void LinkPaymentOrder_OnACapturedBill_ShouldThrow_BLP_BIL34()
    {
        var bill = BillMother.Capture();

        var ex = Assert.Throws<DomainException>(
            () => bill.LinkPaymentOrder(OrderId, ScheduleFor, DecidedAt));

        Assert.Equal("BLP.BIL34", ex.Id);
    }

    // O dinheiro saiu: Scheduled → Paid, terminal.
    [Fact]
    public void MarkPaid_OnAScheduledBill_ShouldBecomeTerminalPaid()
    {
        var bill = Scheduled();

        bill.MarkPaid(DecidedAt);

        Assert.Equal(BillStatus.Paid, bill.Status);
        Assert.True(bill.Status.IsTerminal);
    }

    // A execução falhou: Scheduled → Failed. Os motivos vivem na ordem, não aqui.
    [Fact]
    public void MarkFailed_OnAScheduledBill_ShouldReflectTheFailure()
    {
        var bill = Scheduled();

        bill.MarkFailed(DecidedAt);

        Assert.Equal(BillStatus.Failed, bill.Status);
    }

    // Cancelamento pós-agendamento reflete sem tocar a trilha de aprovação — quem cancelou e
    // por quê vive na ordem.
    [Fact]
    public void MarkScheduleCancelled_ShouldCancelWithoutTouchingTheApprovalRecord()
    {
        var bill = Scheduled();
        var approval = bill.Approval;

        bill.MarkScheduleCancelled(DecidedAt);

        Assert.Equal(BillStatus.Cancelled, bill.Status);
        Assert.Same(approval, bill.Approval);
    }

    // A nova tentativa é uma nova aprovação e uma nova ordem (ADR-002): reabrir limpa o vínculo
    // e a data, e devolve o boleto à fila de decisão.
    [Fact]
    public void ReopenForApproval_AfterAFailure_ShouldClearTheOrderLink()
    {
        var bill = Scheduled();
        bill.MarkFailed(DecidedAt);

        bill.ReopenForApproval(DecidedAt);

        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
        Assert.Null(bill.PaymentOrderId);
        Assert.Null(bill.ScheduledFor);
    }

    // Refletir pagamento num boleto pago é reentrega do outbox — recusada como conflito.
    [Fact]
    public void MarkPaid_OnAPaidBill_ShouldThrow_BLP_BIL34()
    {
        var bill = Scheduled();
        bill.MarkPaid(DecidedAt);

        var ex = Assert.Throws<DomainException>(() => bill.MarkPaid(DecidedAt));

        Assert.Equal("BLP.BIL34", ex.Id);
    }

    // ADR-017: boleto já vencido não é aprovado em silêncio — o provedor o pagaria NA HORA, sem
    // janela de reação, e a aprovação exige o aceite explícito.
    [Fact]
    public void Approve_AnOverdueBillWithoutTheImmediateAcknowledgment_ShouldThrow_BLP_BIL35()
    {
        var bill = ReadyForApproval();
        var overdueToday = new DateOnly(2026, 7, 5);

        var ex = Assert.Throws<DomainException>(() => bill.Approve(
            Approver, new DateOnly(2026, 7, 6), null, ApprovalPolicy.Default(null),
            RiskLevel.ExtremeDanger, overdueToday, DecidedAt));

        Assert.Equal("BLP.BIL35", ex.Id);
        Assert.Equal(BillStatus.AwaitingApproval, bill.Status);
    }

    // Com o aceite, o vencido é aprovável — e é esse consentimento que a ordem herda para não
    // parar na fila perguntando de novo.
    [Fact]
    public void Approve_AnOverdueBillWithTheImmediateAcknowledgment_ShouldSucceed()
    {
        var bill = ReadyForApproval();
        var overdueToday = new DateOnly(2026, 7, 5);

        bill.Approve(
            Approver, new DateOnly(2026, 7, 6), null, ApprovalPolicy.Default(null),
            RiskLevel.ExtremeDanger, overdueToday, DecidedAt,
            acknowledgeRisk: false, acknowledgeImmediateExecution: true);

        Assert.Equal(BillStatus.Approved, bill.Status);
    }

    // Contraprova: boleto dentro do prazo não exige o aceite de execução imediata.
    [Fact]
    public void Approve_ABillDueInTheFuture_ShouldNotRequireTheImmediateAcknowledgment()
    {
        var bill = ReadyForApproval();

        bill.Approve(
            Approver, ScheduleFor, null, ApprovalPolicy.Default(null),
            RiskLevel.ExtremeDanger, Today, DecidedAt);

        Assert.Equal(BillStatus.Approved, bill.Status);
    }

    private static Bill ReadyForApproval()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.RecordChecks(AllPassing(), DecidedAt);
        bill.PullDomainEvents();
        return bill;
    }

    private static Bill Approved()
    {
        var bill = ReadyForApproval();
        bill.Approve(
            Approver, ScheduleFor, null, ApprovalPolicy.Default(null),
            RiskLevel.ExtremeDanger, Today, DecidedAt);
        bill.PullDomainEvents();
        return bill;
    }

    private static Bill Scheduled()
    {
        var bill = Approved();
        bill.LinkPaymentOrder(OrderId, ScheduleFor, DecidedAt);
        return bill;
    }

    private static List<CheckResult> AllPassing()
        => [.. Enumeration.GetAll<CheckType>().Select(type => CheckResult.Passed(type))];
}
