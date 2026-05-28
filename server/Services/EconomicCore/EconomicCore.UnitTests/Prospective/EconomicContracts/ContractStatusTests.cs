namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;

public class ContractStatusTests
{
    // GetAll retorna Draft/Active/Suspended/Terminated.
    [Fact]
    public void GetAll_ShouldReturnFourMembers()
    {
        var all = Enumeration.GetAll<ContractStatus>().ToList();

        Assert.Equal(4, all.Count);
    }

    // Transições permitidas: Draft→Active, Draft→Terminated, Active↔Suspended, Active→Terminated, Suspended→Terminated. Voltar para Draft nunca é permitido.
    [Theory]
    [InlineData("DRAFT", "ACTIVE", true)]
    [InlineData("DRAFT", "TERMINATED", true)]
    [InlineData("ACTIVE", "SUSPENDED", true)]
    [InlineData("ACTIVE", "TERMINATED", true)]
    [InlineData("SUSPENDED", "ACTIVE", true)]
    [InlineData("SUSPENDED", "TERMINATED", true)]
    [InlineData("DRAFT", "DRAFT", false)]
    [InlineData("DRAFT", "SUSPENDED", false)]
    [InlineData("ACTIVE", "DRAFT", false)]
    [InlineData("ACTIVE", "ACTIVE", false)]
    [InlineData("SUSPENDED", "DRAFT", false)]
    [InlineData("TERMINATED", "DRAFT", false)]
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
