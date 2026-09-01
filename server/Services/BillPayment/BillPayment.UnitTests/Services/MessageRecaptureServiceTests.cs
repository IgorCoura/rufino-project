namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Bills.Mothers;
using BillPayment.UnitTests.CaptureItems.Mothers;
using BillPayment.UnitTests.CapturedMessages.Mothers;
using BillPayment.UnitTests.Services.Mothers;

/// <summary>
/// O que a recaptura pode desfazer: boleto ainda não decidido é cancelado e recriado; boleto com
/// dinheiro comprometido trava tudo; negado só avisa.
/// </summary>
public class MessageRecaptureServiceTests
{
    private static readonly UserId Decider = UserId.From(new Guid("0195a1f0-0000-7000-8000-00000000000a"));
    // As mesmas datas de BillApprovalTests: o retrato da ValidationMother é "fresco" nelas.
    private static readonly DateTime DecidedAt = new(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 6, 20);

    // Boleto aprovado trava a recaptura inteira — BLP.CMS11 — e nada é planejado.
    [Fact]
    public void Plan_WhenALinkedBillIsApproved_Throws_BLP_CMS11()
    {
        var bill = AwaitingApproval();
        bill.Approve(Decider, Today.AddDays(3), null, ApprovalPolicy.Default(null), RiskLevel.ExtremeDanger, Today, DecidedAt);

        var exception = Assert.Throws<DomainException>(() =>
            MessageRecaptureService.Plan(CapturedMessageMother.Register(), [(PromotedTo(bill), bill)]));

        Assert.Equal("BLP.CMS11", exception.Id);
    }

    // Boleto capturado ou aguardando aprovação é cancelado para a triagem nova recriá-lo.
    [Fact]
    public void Plan_WhenALinkedBillIsStillUndecided_ShouldScheduleItForCancellation()
    {
        var captured = BillMother.Capture();
        var awaiting = AwaitingApproval();

        var plan = MessageRecaptureService.Plan(
            CapturedMessageMother.Register(),
            [(PromotedTo(captured), captured), (PromotedTo(awaiting), awaiting)]);

        Assert.Equal(2, plan.BillsToCancel.Count);
        Assert.Contains(captured, plan.BillsToCancel);
        Assert.Contains(awaiting, plan.BillsToCancel);
        Assert.Empty(plan.PreviouslyDeniedBills);
    }

    // Boleto negado não bloqueia nem é cancelado — mas quem pediu a recaptura é avisado.
    [Fact]
    public void Plan_WhenALinkedBillWasDenied_ShouldNotBlockButReportIt()
    {
        var bill = AwaitingApproval();
        bill.Deny(Decider, "duplicado", DecidedAt);

        var plan = MessageRecaptureService.Plan(CapturedMessageMother.Register(), [(PromotedTo(bill), bill)]);

        Assert.Empty(plan.BillsToCancel);
        Assert.Equal(bill.Id, Assert.Single(plan.PreviouslyDeniedBills));
    }

    // Boleto já cancelado não pede nada.
    [Fact]
    public void Plan_WhenALinkedBillWasCancelled_ShouldPlanNothingForIt()
    {
        var bill = BillMother.Capture();
        bill.Cancel(Decider, "engano", DecidedAt);

        var plan = MessageRecaptureService.Plan(CapturedMessageMother.Register(), [(PromotedTo(bill), bill)]);

        Assert.Empty(plan.BillsToCancel);
        Assert.Empty(plan.PreviouslyDeniedBills);
    }

    // Só conta o boleto que ESTE anexo produziu: item apontando para outro boleto (reenvio que
    // apontou para o original de outro e-mail) não pode cancelar o boleto alheio.
    [Fact]
    public void Plan_WhenTheItemPointsToADifferentBill_ShouldIgnoreThatBill()
    {
        var bill = AwaitingApproval();
        var item = CaptureItemMother.Unrouted();
        item.Claim(Decider, CaptureItemMother.DefaultBill, DecidedAt);

        var plan = MessageRecaptureService.Plan(CapturedMessageMother.Register(), [(item, bill)]);

        Assert.Empty(plan.BillsToCancel);
    }

    // Sem identificador do cabeçalho não há recaptura — a guarda do agregado é a primeira.
    [Fact]
    public void Plan_WithoutAnInternetMessageId_Throws_BLP_CMS08()
    {
        var exception = Assert.Throws<DomainException>(() =>
            MessageRecaptureService.Plan(CapturedMessageMother.Register(internetMessageId: null), []));

        Assert.Equal("BLP.CMS08", exception.Id);
    }

    private static Bill AwaitingApproval()
    {
        var bill = ValidationMother.BankSlipWithLookup();
        bill.RecordChecks(
            [.. Enumeration.GetAll<CheckType>().Select(type => CheckResult.Passed(type))],
            DecidedAt);
        bill.PullDomainEvents();
        return bill;
    }

    private static CaptureItem PromotedTo(Bill bill)
    {
        var item = CaptureItemMother.Unrouted();
        item.Claim(Decider, bill.Id, DecidedAt);
        return item;
    }
}
