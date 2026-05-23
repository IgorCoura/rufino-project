namespace EconomicCore.UnitTests.Operational.EconomicAgents;

using EconomicCore.Domain.Operational.EconomicAgents.Enumerations;
using EconomicCore.Domain.SeedWork;

public class AgentScopeTests
{
    // GetAll retorna exatamente Inside e Outside.
    [Fact]
    public void GetAll_ShouldReturnInsideAndOutside()
    {
        var all = Enumeration.GetAll<AgentScope>().ToList();

        Assert.Equal(2, all.Count);
        Assert.Contains(AgentScope.Inside, all);
        Assert.Contains(AgentScope.Outside, all);
    }

    // Inside tem Id=1 e Name="INSIDE"; Outside tem Id=2 e Name="OUTSIDE".
    [Fact]
    public void Members_ShouldHaveExpectedIdAndName()
    {
        Assert.Equal(1, AgentScope.Inside.Id);
        Assert.Equal("INSIDE", AgentScope.Inside.Name);
        Assert.Equal(2, AgentScope.Outside.Id);
        Assert.Equal("OUTSIDE", AgentScope.Outside.Name);
    }

    // FromValue resolve por Id para a instância correspondente.
    [Theory]
    [InlineData(1, "INSIDE")]
    [InlineData(2, "OUTSIDE")]
    public void FromValue_ShouldResolveToMatchingScope(int id, string expectedName)
    {
        var scope = Enumeration.FromValue<AgentScope>(id);

        Assert.Equal(expectedName, scope.Name);
    }

    // TryFromValue com Id inexistente retorna null sem lançar.
    [Fact]
    public void TryFromValue_WithUnknownId_ShouldReturnNull()
    {
        var result = Enumeration.TryFromValue<AgentScope>(99);

        Assert.Null(result);
    }
}
