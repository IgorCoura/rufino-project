namespace EconomicCore.UnitTests.Operational.EconomicEvents;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public class DualityLinkTests
{
    private static readonly EconomicEventId ValidCounterpart = EconomicEventId.From(new Guid("dddddddd-dddd-7ddd-8ddd-dddddddddddd"));

    // Construção válida preserva CounterpartEventId e MatchedAmount.
    [Fact]
    public void Constructor_WithValidInputs_ShouldStoreValues()
    {
        var link = new DualityLink(ValidCounterpart, new Money(500m, Currency.BRL));

        Assert.Equal(ValidCounterpart, link.CounterpartEventId);
        Assert.Equal(500m, link.MatchedAmount.Amount);
    }

    // CounterpartEventId vazio lança ECC.EVT11.
    [Fact]
    public void Constructor_WithEmptyCounterpart_ShouldThrowECC_EVT11()
    {
        var ex = Assert.Throws<DomainException>(
            () => new DualityLink(EconomicEventId.Empty, new Money(100m, Currency.BRL)));

        Assert.Equal("ECC.EVT11", ex.Id);
    }

    // MatchedAmount null ou ≤ 0 lança ECC.EVT12.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveAmount_ShouldThrowECC_EVT12(double amount)
    {
        var ex = Assert.Throws<DomainException>(
            () => new DualityLink(ValidCounterpart, new Money((decimal)amount, Currency.BRL)));

        Assert.Equal("ECC.EVT12", ex.Id);
    }

    // Dois DualityLinks com mesmos componentes são iguais.
    [Fact]
    public void Equals_SameComponents_ShouldBeTrue()
    {
        var a = new DualityLink(ValidCounterpart, new Money(500m, Currency.BRL));
        var b = new DualityLink(ValidCounterpart, new Money(500m, Currency.BRL));

        Assert.Equal(a, b);
    }
}
