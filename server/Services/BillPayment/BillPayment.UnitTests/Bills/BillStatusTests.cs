namespace BillPayment.UnitTests.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.SeedWork;

/// <summary>
/// A máquina de estados inteira está declarada antes de os métodos que a percorrem
/// existirem. Estes testes a congelam agora — sem eles, um erro na matriz ficaria dormente
/// até a sprint 1.5 e apareceria como bug de aprovação.
/// </summary>
public class BillStatusTests
{
    // Cada transição prevista no modelo é aceita.
    [Theory]
    [InlineData("Captured", "AwaitingApproval")]
    [InlineData("Captured", "Rejected")]
    [InlineData("Captured", "Cancelled")]
    [InlineData("Rejected", "AwaitingApproval")]
    [InlineData("Rejected", "Cancelled")]
    [InlineData("AwaitingApproval", "Approved")]
    [InlineData("AwaitingApproval", "Denied")]
    [InlineData("AwaitingApproval", "Cancelled")]
    [InlineData("Approved", "Scheduled")]
    [InlineData("Approved", "Cancelled")]
    [InlineData("Scheduled", "Paid")]
    [InlineData("Scheduled", "Failed")]
    [InlineData("Scheduled", "Cancelled")]
    [InlineData("Failed", "AwaitingApproval")]
    [InlineData("Failed", "Cancelled")]
    public void CanTransitionTo_WithModelledTransition_ShouldBeAllowed(string from, string to)
    {
        var source = Enumeration.FromDisplayName<BillStatus>(from);
        var target = Enumeration.FromDisplayName<BillStatus>(to);

        Assert.True(source.CanTransitionTo(target));
    }

    // Atalhos que pulariam verificação ou aprovação são recusados.
    [Theory]
    [InlineData("Captured", "Approved")]
    [InlineData("Captured", "Paid")]
    [InlineData("Captured", "Scheduled")]
    [InlineData("AwaitingApproval", "Paid")]
    [InlineData("AwaitingApproval", "Scheduled")]
    [InlineData("Approved", "Paid")]
    [InlineData("Rejected", "Approved")]
    [InlineData("Scheduled", "Approved")]
    [InlineData("Failed", "Paid")]
    public void CanTransitionTo_WithShortcutThatSkipsAStep_ShouldBeRejected(string from, string to)
    {
        var source = Enumeration.FromDisplayName<BillStatus>(from);
        var target = Enumeration.FromDisplayName<BillStatus>(to);

        Assert.False(source.CanTransitionTo(target));
    }

    // Estado terminal não sai de lugar nenhum — é o que sustenta BLP.BIL07.
    [Theory]
    [InlineData("Paid")]
    [InlineData("Denied")]
    [InlineData("Cancelled")]
    public void CanTransitionTo_FromTerminalStatus_ShouldAlwaysBeRejected(string from)
    {
        var source = Enumeration.FromDisplayName<BillStatus>(from);

        Assert.True(source.IsTerminal);
        Assert.All(Enumeration.GetAll<BillStatus>(), target => Assert.False(source.CanTransitionTo(target)));
    }

    // Nenhum estado transita para si mesmo — repetir a operação não é transição.
    [Fact]
    public void CanTransitionTo_ToItself_ShouldBeRejectedForEveryStatus()
    {
        Assert.All(Enumeration.GetAll<BillStatus>(), status => Assert.False(status.CanTransitionTo(status)));
    }

    // Alvo nulo não derruba a checagem.
    [Fact]
    public void CanTransitionTo_WithNullTarget_ShouldBeRejected()
    {
        Assert.False(BillStatus.Captured.CanTransitionTo(null!));
    }

    // Denied e Cancelled liberam a chave natural: o compromisso não vai ser pago por eles,
    // e o documento pode legitimamente ser reimportado depois.
    [Theory]
    [InlineData("Denied")]
    [InlineData("Cancelled")]
    public void OccupiesNaturalKey_WhenTheBillWillNeverBePaid_ShouldBeFalse(string name)
    {
        Assert.False(Enumeration.FromDisplayName<BillStatus>(name).OccupiesNaturalKey);
    }

    // Paid NÃO libera a chave: é justamente a duplicata de algo já pago que precisa ser barrada.
    [Theory]
    [InlineData("Captured")]
    [InlineData("AwaitingApproval")]
    [InlineData("Rejected")]
    [InlineData("Approved")]
    [InlineData("Scheduled")]
    [InlineData("Failed")]
    [InlineData("Paid")]
    public void OccupiesNaturalKey_WhileTheCommitmentIsStillLive_ShouldBeTrue(string name)
    {
        Assert.True(Enumeration.FromDisplayName<BillStatus>(name).OccupiesNaturalKey);
    }
}
