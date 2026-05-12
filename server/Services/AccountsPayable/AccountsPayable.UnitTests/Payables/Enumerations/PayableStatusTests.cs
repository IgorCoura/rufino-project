namespace AccountsPayable.UnitTests.Payables.Enumerations;

using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.SeedWork;

public class PayableStatusTests
{
    // Matriz completa de transições do Payable:
    // Válidas: Draft → {Scheduled, Paid, Cancelled}, Scheduled → {Paid, Cancelled}.
    // Inválidas: tudo a partir de Paid ou Cancelled (terminais), self-transitions, e Scheduled → Draft.
    [Theory]
    [InlineData(1, 2, true)]   // Draft -> Scheduled
    [InlineData(1, 3, true)]   // Draft -> Paid (pode pagar antes de agendar)
    [InlineData(1, 4, true)]   // Draft -> Cancelled
    [InlineData(2, 3, true)]   // Scheduled -> Paid
    [InlineData(2, 4, true)]   // Scheduled -> Cancelled
    [InlineData(1, 1, false)]  // self (Draft)
    [InlineData(2, 2, false)]  // self (Scheduled)
    [InlineData(3, 3, false)]  // self (Paid)
    [InlineData(4, 4, false)]  // self (Cancelled)
    [InlineData(2, 1, false)]  // Scheduled -> Draft (não permite voltar)
    [InlineData(3, 1, false)]  // Paid terminal
    [InlineData(3, 2, false)]
    [InlineData(3, 4, false)]
    [InlineData(4, 1, false)]  // Cancelled terminal
    [InlineData(4, 2, false)]
    [InlineData(4, 3, false)]
    public void CanTransitionTo_ShouldReturnExpectedResult(int fromId, int toId, bool expected)
    {
        var from = Enumeration.FromValue<PayableStatus>(fromId);
        var to = Enumeration.FromValue<PayableStatus>(toId);

        var result = from.CanTransitionTo(to);

        Assert.Equal(expected, result);
    }

    // GetAll retorna exatamente os 4 status declarados.
    [Fact]
    public void GetAll_ShouldReturnAllFourStatuses()
    {
        var all = Enumeration.GetAll<PayableStatus>().ToList();

        Assert.Equal(4, all.Count);
        Assert.Contains(PayableStatus.Draft, all);
        Assert.Contains(PayableStatus.Scheduled, all);
        Assert.Contains(PayableStatus.Paid, all);
        Assert.Contains(PayableStatus.Cancelled, all);
    }
}
