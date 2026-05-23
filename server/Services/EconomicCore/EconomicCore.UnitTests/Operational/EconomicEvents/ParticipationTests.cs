namespace EconomicCore.UnitTests.Operational.EconomicEvents;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.SeedWork;

public class ParticipationTests
{
    private static readonly EconomicAgentId ValidAgentId = EconomicAgentId.From(new Guid("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa"));

    // Construção válida preserva AgentId e Role.
    [Fact]
    public void Constructor_WithValidAgentAndRole_ShouldStoreValues()
    {
        var p = new Participation(ValidAgentId, ParticipationRole.Provider);

        Assert.Equal(ValidAgentId, p.AgentId);
        Assert.Same(ParticipationRole.Provider, p.Role);
    }

    // EconomicAgentId vazio (Empty) lança ECC.EVT08.
    [Fact]
    public void Constructor_WithEmptyAgentId_ShouldThrowECC_EVT08()
    {
        var ex = Assert.Throws<DomainException>(() => new Participation(EconomicAgentId.Empty, ParticipationRole.Provider));

        Assert.Equal("ECC.EVT08", ex.Id);
    }

    // Role null lança ECC.EVT09.
    [Fact]
    public void Constructor_WithNullRole_ShouldThrowECC_EVT09()
    {
        var ex = Assert.Throws<DomainException>(() => new Participation(ValidAgentId, null!));

        Assert.Equal("ECC.EVT09", ex.Id);
    }

    // Duas Participations com mesmo Agent+Role são iguais.
    [Fact]
    public void Equals_SameComponents_ShouldBeTrue()
    {
        var a = new Participation(ValidAgentId, ParticipationRole.Provider);
        var b = new Participation(ValidAgentId, ParticipationRole.Provider);

        Assert.Equal(a, b);
    }

    // Roles diferentes (mesmo Agent) produzem instâncias não iguais.
    [Fact]
    public void Equals_DifferentRole_ShouldBeFalse()
    {
        var a = new Participation(ValidAgentId, ParticipationRole.Provider);
        var b = new Participation(ValidAgentId, ParticipationRole.Recipient);

        Assert.NotEqual(a, b);
    }
}
