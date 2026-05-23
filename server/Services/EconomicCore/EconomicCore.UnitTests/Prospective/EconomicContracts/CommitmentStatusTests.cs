namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;

public class CommitmentStatusTests
{
    // GetAll retorna 5 valores: Promised, Reserved, Fulfilled, Expired, Cancelled.
    [Fact]
    public void GetAll_ShouldReturnFiveMembers()
    {
        var all = Enumeration.GetAll<CommitmentStatus>().ToList();

        Assert.Equal(5, all.Count);
    }

    // Transições permitidas a partir de Promised: Reserved, Fulfilled, Expired, Cancelled.
    // De Reserved: Fulfilled, Expired, Cancelled. Estados terminais não saem.
    [Theory]
    [InlineData("PROMISED", "RESERVED", true)]
    [InlineData("PROMISED", "FULFILLED", true)]
    [InlineData("PROMISED", "EXPIRED", true)]
    [InlineData("PROMISED", "CANCELLED", true)]
    [InlineData("RESERVED", "FULFILLED", true)]
    [InlineData("RESERVED", "EXPIRED", true)]
    [InlineData("RESERVED", "CANCELLED", true)]
    [InlineData("PROMISED", "PROMISED", false)]
    [InlineData("FULFILLED", "PROMISED", false)]
    [InlineData("EXPIRED", "FULFILLED", false)]
    [InlineData("CANCELLED", "RESERVED", false)]
    public void CanTransitionTo_ShouldFollowAllowedMatrix(string fromName, string toName, bool expected)
    {
        var from = Enumeration.FromDisplayName<CommitmentStatus>(fromName);
        var to = Enumeration.FromDisplayName<CommitmentStatus>(toName);

        Assert.Equal(expected, from.CanTransitionTo(to));
    }

    // Estados terminais (Fulfilled, Expired, Cancelled) reportam IsTerminal=true.
    [Theory]
    [InlineData("FULFILLED", true)]
    [InlineData("EXPIRED", true)]
    [InlineData("CANCELLED", true)]
    [InlineData("PROMISED", false)]
    [InlineData("RESERVED", false)]
    public void IsTerminal_ShouldReportTrueOnlyForTerminalStates(string name, bool expected)
    {
        var status = Enumeration.FromDisplayName<CommitmentStatus>(name);

        Assert.Equal(expected, status.IsTerminal);
    }
}
