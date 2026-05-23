namespace EconomicCore.UnitTests.Operational.EconomicEvents;

using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.SeedWork;

public class FlowDirectionTests
{
    // GetAll retorna Inflow e Outflow (2 valores).
    [Fact]
    public void GetAll_ShouldReturnInflowAndOutflow()
    {
        var all = Enumeration.GetAll<FlowDirection>().ToList();

        Assert.Equal(2, all.Count);
        Assert.Contains(FlowDirection.Inflow, all);
        Assert.Contains(FlowDirection.Outflow, all);
    }

    // Cada membro tem Id e Name esperados.
    [Theory]
    [InlineData(1, "INFLOW")]
    [InlineData(2, "OUTFLOW")]
    public void Members_ShouldHaveExpectedIdAndName(int id, string name)
    {
        var direction = Enumeration.FromValue<FlowDirection>(id);

        Assert.Equal(id, direction.Id);
        Assert.Equal(name, direction.Name);
    }
}
