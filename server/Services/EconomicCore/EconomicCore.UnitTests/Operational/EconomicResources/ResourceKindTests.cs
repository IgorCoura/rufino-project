namespace EconomicCore.UnitTests.Operational.EconomicResources;

using EconomicCore.Domain.Operational.EconomicResources.Enumerations;
using EconomicCore.Domain.SeedWork;

public class ResourceKindTests
{
    // GetAll retorna exatamente os 4 valores definidos no Smart Enum.
    [Fact]
    public void GetAll_ShouldReturnFourMembers()
    {
        var all = Enumeration.GetAll<ResourceKind>().ToList();

        Assert.Equal(4, all.Count);
        Assert.Contains(ResourceKind.Cash, all);
        Assert.Contains(ResourceKind.Service, all);
        Assert.Contains(ResourceKind.LaborService, all);
        Assert.Contains(ResourceKind.FiscalObligation, all);
    }

    // Cada instância tem o Id e Name esperados (códigos em UPPER_SNAKE).
    [Theory]
    [InlineData(1, "CASH")]
    [InlineData(2, "SERVICE")]
    [InlineData(3, "LABOR_SERVICE")]
    [InlineData(4, "FISCAL_OBLIGATION")]
    public void Members_ShouldHaveExpectedIdAndName(int id, string expectedName)
    {
        var kind = Enumeration.FromValue<ResourceKind>(id);

        Assert.Equal(id, kind.Id);
        Assert.Equal(expectedName, kind.Name);
    }

    // TryFromValue com Id inexistente retorna null sem lançar.
    [Fact]
    public void TryFromValue_WithUnknownId_ShouldReturnNull()
    {
        var result = Enumeration.TryFromValue<ResourceKind>(99);

        Assert.Null(result);
    }
}
