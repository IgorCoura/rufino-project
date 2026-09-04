namespace BillPayment.UnitTests.PaymentOrders;

using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;

/// <summary>
/// A máquina de estados da execução financeira, congelada por completo — do <c>Pending</c> em
/// diante quem dita o estado é o provedor, e é esta matriz que sustenta a monotonia de
/// <c>ApplyProviderStatus</c>: uma aresta a mais aqui é uma regressão de dinheiro possível lá.
/// </summary>
public class PaymentOrderStatusTests
{
    // Cada transição prevista no modelo é aceita — a matriz válida completa.
    [Theory]
    [InlineData("Draft", "Pending")]
    [InlineData("Draft", "Failed")]
    [InlineData("Draft", "Cancelled")]
    [InlineData("Pending", "BankProcessing")]
    [InlineData("Pending", "Paid")]
    [InlineData("Pending", "Failed")]
    [InlineData("Pending", "Cancelled")]
    [InlineData("Pending", "Refunded")]
    [InlineData("BankProcessing", "Paid")]
    [InlineData("BankProcessing", "Failed")]
    [InlineData("BankProcessing", "Cancelled")]
    [InlineData("BankProcessing", "Refunded")]
    [InlineData("Paid", "Refunded")]
    public void CanTransitionTo_WithModelledTransition_ShouldBeAllowed(string from, string to)
    {
        var source = Enumeration.FromDisplayName<PaymentOrderStatus>(from);
        var target = Enumeration.FromDisplayName<PaymentOrderStatus>(to);

        Assert.True(source.CanTransitionTo(target));
    }

    // Atalho ou regressão fora do modelo é recusado — inclusive Paid voltando para trás.
    [Theory]
    [InlineData("Draft", "BankProcessing")]
    [InlineData("Draft", "Paid")]
    [InlineData("Draft", "Refunded")]
    [InlineData("Pending", "Draft")]
    [InlineData("BankProcessing", "Draft")]
    [InlineData("BankProcessing", "Pending")]
    [InlineData("Paid", "Draft")]
    [InlineData("Paid", "Pending")]
    [InlineData("Paid", "BankProcessing")]
    [InlineData("Paid", "Failed")]
    [InlineData("Paid", "Cancelled")]
    public void CanTransitionTo_OutsideTheModel_ShouldBeRejected(string from, string to)
    {
        var source = Enumeration.FromDisplayName<PaymentOrderStatus>(from);
        var target = Enumeration.FromDisplayName<PaymentOrderStatus>(to);

        Assert.False(source.CanTransitionTo(target));
    }

    // Estado terminal não sai de lugar nenhum — Failed é terminal NA ORDEM de propósito
    // (ADR-002): nova tentativa é ordem nova, nunca a mesma ressuscitada.
    [Theory]
    [InlineData("Failed")]
    [InlineData("Cancelled")]
    [InlineData("Refunded")]
    public void CanTransitionTo_FromTerminalStatus_ShouldAlwaysBeRejected(string from)
    {
        var source = Enumeration.FromDisplayName<PaymentOrderStatus>(from);

        Assert.True(source.IsTerminal);
        Assert.All(
            Enumeration.GetAll<PaymentOrderStatus>(),
            target => Assert.False(source.CanTransitionTo(target)));
    }

    // Os quatro estados vivos não são terminais — inclusive Paid, que ainda aceita o estorno.
    [Theory]
    [InlineData("Draft")]
    [InlineData("Pending")]
    [InlineData("BankProcessing")]
    [InlineData("Paid")]
    public void IsTerminal_OnALiveStatus_ShouldBeFalse(string name)
    {
        Assert.False(Enumeration.FromDisplayName<PaymentOrderStatus>(name).IsTerminal);
    }

    // Paid é quase-terminal: a ÚNICA saída é o estorno do provedor.
    [Fact]
    public void CanTransitionTo_FromPaid_ShouldAcceptOnlyRefunded()
    {
        Assert.All(
            Enumeration.GetAll<PaymentOrderStatus>(),
            target => Assert.Equal(
                target == PaymentOrderStatus.Refunded,
                PaymentOrderStatus.Paid.CanTransitionTo(target)));
    }

    // Nenhum estado transita para si mesmo — repetir o retrato do provedor não é transição.
    [Fact]
    public void CanTransitionTo_ToItself_ShouldBeRejectedForEveryStatus()
    {
        Assert.All(
            Enumeration.GetAll<PaymentOrderStatus>(),
            status => Assert.False(status.CanTransitionTo(status)));
    }

    // Alvo nulo não derruba a checagem — o mapa de status do provedor pode devolver nada.
    [Fact]
    public void CanTransitionTo_WithNullTarget_ShouldBeRejected()
    {
        Assert.False(PaymentOrderStatus.Draft.CanTransitionTo(null!));
    }

    // A conciliação por polling vigia exatamente quem espera desfecho do provedor:
    // Pending e BankProcessing, e mais ninguém.
    [Theory]
    [InlineData("Draft", false)]
    [InlineData("Pending", true)]
    [InlineData("BankProcessing", true)]
    [InlineData("Paid", false)]
    [InlineData("Failed", false)]
    [InlineData("Cancelled", false)]
    [InlineData("Refunded", false)]
    public void AwaitsProviderOutcome_ShouldBeTrueOnlyWhileTheProviderStillDecides(string name, bool expected)
    {
        Assert.Equal(expected, Enumeration.FromDisplayName<PaymentOrderStatus>(name).AwaitsProviderOutcome);
    }
}
