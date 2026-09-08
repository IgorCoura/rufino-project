namespace BillPayment.UnitTests.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Services.Mothers;

/// <summary>
/// A separação entre autorizar e mandar executar (ADR-018).
/// </summary>
/// <remarks>
/// O que os testes daqui protegem, e que os de <c>BillApprovalTests</c> não alcançam: que
/// aprovar sozinho <strong>não move dinheiro</strong> — nem cria data, nem emite o evento que a
/// fase 3 consome para criar a ordem.
/// </remarks>
public class BillSchedulingTests
{
    private static readonly UserId Approver = UserId.From(new Guid("0195a1f0-0000-7000-8000-00000000000a"));
    private static readonly UserId Scheduler = UserId.From(new Guid("0195a1f0-0000-7000-8000-00000000000b"));
    private static readonly DateTime DecidedAt = new(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 6, 20);
    private static readonly DateOnly ScheduleFor = new(2026, 6, 24);

    [Fact]
    public void Approve_WithoutScheduling_ShouldNotProduceADateNorASchedulingEvent()
    {
        var bill = ReadyForApproval();

        bill.Approve(Approver, "confere", Policy(), RiskLevel.ExtremeDanger, DecidedAt);

        Assert.Equal(BillStatus.Approved, bill.Status);

        // A ausência de data É o estado novo: é ela que põe o boleto na aba de aprovados
        // esperando alguém agendar, em vez de mandá-lo direto para a fila de pagamento.
        Assert.Null(bill.ScheduledFor);

        var events = bill.PullDomainEvents();
        Assert.Single(events);
        Assert.IsType<BillApprovedDomainEvent>(events[0]);
        Assert.Empty(events.OfType<BillSchedulingRequestedDomainEvent>());
    }

    [Fact]
    public void Schedule_OnAnApprovedBill_ShouldStampTheDateAndEmitTheSchedulingEvent()
    {
        var bill = Approved();

        bill.Schedule(Scheduler, ScheduleFor, Policy(), Today, DecidedAt);

        Assert.Equal(ScheduleFor, bill.ScheduledFor);

        // Continua Approved: quem move para Scheduled é o provedor aceitando a ordem (ADR-002).
        Assert.Equal(BillStatus.Approved, bill.Status);

        var scheduled = Assert.Single(bill.PullDomainEvents().OfType<BillSchedulingRequestedDomainEvent>());
        Assert.Equal(Scheduler, scheduled.RequestedBy);
        Assert.Equal(ScheduleFor, scheduled.ScheduleFor);
    }

    // Quem agenda pode não ser quem aprovou — são alçadas diferentes, e a trilha guarda os dois.
    [Fact]
    public void Schedule_ByADifferentPerson_ShouldRecordBothInTheHistory()
    {
        var bill = Approved();

        bill.Schedule(Scheduler, ScheduleFor, Policy(), Today, DecidedAt, requesterName: "Maria");

        var approvedEntry = Assert.Single(bill.History, h => h.Action == BillAction.Approved);
        var scheduledEntry = Assert.Single(bill.History, h => h.Action == BillAction.Scheduled);

        Assert.Equal(Approver, approvedEntry.ActorUserId);
        Assert.Equal(Scheduler, scheduledEntry.ActorUserId);
        Assert.Equal("Maria", scheduledEntry.ActorName);
    }

    [Fact]
    public void Schedule_OnABillThatWasNeverApproved_ShouldThrow_BLP_BIL36()
    {
        var bill = ReadyForApproval();

        var ex = Assert.Throws<DomainException>(
            () => bill.Schedule(Scheduler, ScheduleFor, Policy(), Today, DecidedAt));

        Assert.Equal("BLP.BIL36", ex.Id);
        Assert.Null(bill.ScheduledFor);
    }

    // Duplo clique não vira duas ordens: a data já gravada basta para recusar.
    [Fact]
    public void Schedule_Twice_ShouldThrow_BLP_BIL37()
    {
        var bill = Approved();
        bill.Schedule(Scheduler, ScheduleFor, Policy(), Today, DecidedAt);

        var ex = Assert.Throws<DomainException>(
            () => bill.Schedule(Scheduler, ScheduleFor.AddDays(1), Policy(), Today, DecidedAt));

        Assert.Equal("BLP.BIL37", ex.Id);
        Assert.Equal(ScheduleFor, bill.ScheduledFor);
    }

    // O frescor do retrato é reconferido NO AGENDAMENTO, e não é redundância: separar os atos
    // abriu uma janela entre eles, e é aqui que o dinheiro anda.
    [Fact]
    public void Schedule_WithAStaleSnapshot_ShouldThrow_BLP_BIL06()
    {
        var bill = Approved();

        var ex = Assert.Throws<DomainException>(
            () => bill.Schedule(Scheduler, ScheduleFor, Policy(), Today, DecidedAt.AddHours(30)));

        Assert.Equal("BLP.BIL06", ex.Id);
        Assert.Null(bill.ScheduledFor);
    }

    private static ApprovalPolicy Policy() => ApprovalPolicy.Default(null);

    private static List<CheckResult> AllPassing()
        => [.. Enumeration.GetAll<CheckType>().Select(type => CheckResult.Passed(type))];

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
        bill.Approve(Approver, null, Policy(), RiskLevel.ExtremeDanger, DecidedAt);
        bill.PullDomainEvents();
        return bill;
    }
}
