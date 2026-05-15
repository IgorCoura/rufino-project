namespace AccountsPayable.UnitTests.Payables.Enumerations;

using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.SeedWork;

public class PayableStatusTests
{
    // Matriz completa de transições do Payable (Sprints 2 + 5 + 6):
    // Status: 1=Draft, 2=Scheduled, 3=Paid, 4=Cancelled, 5=AwaitingApproval, 6=Approved, 7=Rejected,
    //         8=PaymentRequested, 9=PaymentFailed.
    // Terminais: Paid (3), Cancelled (4), Rejected (7) — nenhuma transição sai deles.
    [Theory]
    // Válidas a partir de Draft
    [InlineData(1, 2, true)]   // Draft -> Scheduled
    [InlineData(1, 3, true)]   // Draft -> Paid          (pagamento direto)
    [InlineData(1, 4, true)]   // Draft -> Cancelled
    [InlineData(1, 5, true)]   // Draft -> AwaitingApproval (RequestApproval)
    // Válidas a partir de Scheduled
    [InlineData(2, 3, true)]   // Scheduled -> Paid
    [InlineData(2, 4, true)]   // Scheduled -> Cancelled
    [InlineData(2, 8, true)]   // Scheduled -> PaymentRequested (Sprint 6)
    // Válidas a partir de AwaitingApproval
    [InlineData(5, 4, true)]   // AwaitingApproval -> Cancelled
    [InlineData(5, 6, true)]   // AwaitingApproval -> Approved
    [InlineData(5, 7, true)]   // AwaitingApproval -> Rejected
    // Válidas a partir de Approved
    [InlineData(6, 2, true)]   // Approved -> Scheduled
    [InlineData(6, 3, true)]   // Approved -> Paid
    [InlineData(6, 4, true)]   // Approved -> Cancelled
    // Válidas a partir de PaymentRequested (Sprint 6)
    [InlineData(8, 3, true)]   // PaymentRequested -> Paid          (ConfirmPaid)
    [InlineData(8, 9, true)]   // PaymentRequested -> PaymentFailed (MarkPaymentFailed)
    // Válidas a partir de PaymentFailed (Sprint 6, não terminal)
    [InlineData(9, 8, true)]   // PaymentFailed -> PaymentRequested (retry)
    [InlineData(9, 4, true)]   // PaymentFailed -> Cancelled        (desistir após falha)
    // Self-transitions sempre false
    [InlineData(1, 1, false)]
    [InlineData(2, 2, false)]
    [InlineData(3, 3, false)]
    [InlineData(4, 4, false)]
    [InlineData(5, 5, false)]
    [InlineData(6, 6, false)]
    [InlineData(7, 7, false)]
    [InlineData(8, 8, false)]
    [InlineData(9, 9, false)]
    // Voltar de Scheduled é proibido
    [InlineData(2, 1, false)]
    [InlineData(2, 5, false)]  // Scheduled não solicita approval (já passou da etapa)
    // Terminais Paid e Cancelled não saem
    [InlineData(3, 1, false)]
    [InlineData(3, 2, false)]
    [InlineData(3, 4, false)]
    [InlineData(4, 1, false)]
    [InlineData(4, 2, false)]
    [InlineData(4, 3, false)]
    // Rejected é terminal — critério da Sprint 5: Rejected NÃO vira Paid
    [InlineData(7, 1, false)]
    [InlineData(7, 2, false)]
    [InlineData(7, 3, false)]
    [InlineData(7, 6, false)]
    // AwaitingApproval não pula direto para Scheduled/Paid sem passar por Approved
    [InlineData(5, 2, false)]
    [InlineData(5, 3, false)]
    // Approved não volta para Draft/AwaitingApproval/Rejected
    [InlineData(6, 1, false)]
    [InlineData(6, 5, false)]
    [InlineData(6, 7, false)]
    // PaymentRequested NÃO pode ser cancelado direto (Sprint 6: precisa falhar primeiro)
    [InlineData(8, 4, false)]
    // PaymentRequested NÃO volta para Scheduled
    [InlineData(8, 2, false)]
    // PaymentFailed NÃO vira Paid direto (precisa retry → PaymentRequested → ConfirmPaid)
    [InlineData(9, 3, false)]
    // PaymentFailed NÃO volta para Scheduled
    [InlineData(9, 2, false)]
    public void CanTransitionTo_ShouldReturnExpectedResult(int fromId, int toId, bool expected)
    {
        var from = Enumeration.FromValue<PayableStatus>(fromId);
        var to = Enumeration.FromValue<PayableStatus>(toId);

        var result = from.CanTransitionTo(to);

        Assert.Equal(expected, result);
    }

    // GetAll retorna os 9 status declarados (4 da Sprint 2 + 3 da Sprint 5 + 2 da Sprint 6).
    [Fact]
    public void GetAll_ShouldReturnAllNineStatuses()
    {
        var all = Enumeration.GetAll<PayableStatus>().ToList();

        Assert.Equal(9, all.Count);
        Assert.Contains(PayableStatus.Draft, all);
        Assert.Contains(PayableStatus.Scheduled, all);
        Assert.Contains(PayableStatus.Paid, all);
        Assert.Contains(PayableStatus.Cancelled, all);
        Assert.Contains(PayableStatus.AwaitingApproval, all);
        Assert.Contains(PayableStatus.Approved, all);
        Assert.Contains(PayableStatus.Rejected, all);
        Assert.Contains(PayableStatus.PaymentRequested, all);
        Assert.Contains(PayableStatus.PaymentFailed, all);
    }
}
