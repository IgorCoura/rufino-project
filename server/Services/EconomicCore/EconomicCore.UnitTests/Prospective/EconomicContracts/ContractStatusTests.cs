namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;

public class ContractStatusTests
{
    // GetAll retorna Active/Suspended/Terminated.
    [Fact]
    public void GetAll_ShouldReturnThreeMembers()
    {
        var all = Enumeration.GetAll<ContractStatus>().ToList();

        Assert.Equal(3, all.Count);
    }

    // Transições permitidas: Active↔Suspended, Active→Terminated, Suspended→Terminated.
    [Theory]
    [InlineData("ACTIVE", "SUSPENDED", true)]
    [InlineData("ACTIVE", "TERMINATED", true)]
    [InlineData("SUSPENDED", "ACTIVE", true)]
    [InlineData("SUSPENDED", "TERMINATED", true)]
    [InlineData("ACTIVE", "ACTIVE", false)]
    [InlineData("TERMINATED", "ACTIVE", false)]
    [InlineData("TERMINATED", "SUSPENDED", false)]
    [InlineData("TERMINATED", "TERMINATED", false)]
    public void CanTransitionTo_ShouldFollowAllowedMatrix(string fromName, string toName, bool expected)
    {
        var from = Enumeration.FromDisplayName<ContractStatus>(fromName);
        var to = Enumeration.FromDisplayName<ContractStatus>(toName);

        Assert.Equal(expected, from.CanTransitionTo(to));
    }
}
