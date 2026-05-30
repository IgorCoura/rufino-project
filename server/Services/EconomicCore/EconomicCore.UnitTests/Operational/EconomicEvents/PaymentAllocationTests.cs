namespace EconomicCore.UnitTests.Operational.EconomicEvents;

using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public class PaymentAllocationTests
{
    private static CommitmentRef Ref() => new(
        EconomicContractId.From(new Guid("eeeeeeee-eeee-7eee-8eee-eeeeeeeeeeee")),
        CommitmentId.From(new Guid("cccccccc-cccc-7ccc-8ccc-cccccccccccc")));

    // Alocação válida preserva o CommitmentRef e o valor.
    [Fact]
    public void Constructor_WithValidArguments_ShouldExposeComponents()
    {
        var allocation = new PaymentAllocation(Ref(), new Money(500m, Currency.BRL));

        Assert.Equal(Ref(), allocation.Commitment);
        Assert.Equal(500m, allocation.Amount.Amount);
    }

    // CommitmentRef nulo é rejeitado com ECC.EVT16.
    [Fact]
    public void Constructor_WithNullCommitment_ShouldThrowECC_EVT16()
    {
        var ex = Assert.Throws<DomainException>(() => new PaymentAllocation(null!, new Money(500m, Currency.BRL)));

        Assert.Equal("ECC.EVT16", ex.Id);
    }

    // Valor zero ou negativo é rejeitado com ECC.EVT16.
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Constructor_WithNonPositiveAmount_ShouldThrowECC_EVT16(decimal amount)
    {
        var ex = Assert.Throws<DomainException>(() => new PaymentAllocation(Ref(), new Money(amount, Currency.BRL)));

        Assert.Equal("ECC.EVT16", ex.Id);
    }

    // Igualdade por valor: mesmas componentes ⇒ iguais; valor diferente ⇒ diferentes.
    [Fact]
    public void Equality_ShouldCompareByComponents()
    {
        var a = new PaymentAllocation(Ref(), new Money(500m, Currency.BRL));
        var b = new PaymentAllocation(Ref(), new Money(500m, Currency.BRL));
        var c = new PaymentAllocation(Ref(), new Money(600m, Currency.BRL));

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
