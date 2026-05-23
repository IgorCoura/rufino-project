namespace EconomicCore.UnitTests.Operational.EconomicEvents;

using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.SeedWork;

public class ParticipationRoleTests
{
    // GetAll retorna Provider e Recipient (2 valores).
    [Fact]
    public void GetAll_ShouldReturnProviderAndRecipient()
    {
        var all = Enumeration.GetAll<ParticipationRole>().ToList();

        Assert.Equal(2, all.Count);
        Assert.Contains(ParticipationRole.Provider, all);
        Assert.Contains(ParticipationRole.Recipient, all);
    }

    // Cada membro tem Id e Name esperados.
    [Theory]
    [InlineData(1, "PROVIDER")]
    [InlineData(2, "RECIPIENT")]
    public void Members_ShouldHaveExpectedIdAndName(int id, string name)
    {
        var role = Enumeration.FromValue<ParticipationRole>(id);

        Assert.Equal(id, role.Id);
        Assert.Equal(name, role.Name);
    }
}
