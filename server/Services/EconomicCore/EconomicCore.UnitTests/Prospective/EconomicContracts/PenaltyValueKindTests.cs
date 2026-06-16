namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;

public class PenaltyValueKindTests
{
    // GetAll retorna os dois kinds: Percent e FixedAmount.
    [Fact]
    public void GetAll_ShouldReturnTwoMembers()
    {
        var all = Enumeration.GetAll<PenaltyValueKind>().ToList();

        Assert.Equal(2, all.Count);
    }

    // FromDisplayName resolve PERCENT e FIXED para as instâncias canônicas.
    [Theory]
    [InlineData("PERCENT", 1)]
    [InlineData("FIXED", 2)]
    public void FromDisplayName_ShouldResolveCanonicalInstances(string name, int expectedId)
    {
        var kind = Enumeration.FromDisplayName<PenaltyValueKind>(name);

        Assert.Equal(expectedId, kind.Id);
    }

    // FromValue resolve os ids 1 e 2 para Percent e FixedAmount.
    [Fact]
    public void FromValue_ShouldResolveCanonicalInstances()
    {
        Assert.Equal(PenaltyValueKind.Percent, Enumeration.FromValue<PenaltyValueKind>(1));
        Assert.Equal(PenaltyValueKind.FixedAmount, Enumeration.FromValue<PenaltyValueKind>(2));
    }
}
