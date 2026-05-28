namespace EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.SeedWork;

public sealed class Participation : ValueObject
{
    public EconomicAgentId AgentId { get; private set; }
    public ParticipationRole Role { get; private set; } = default!;

    private Participation() { }

    public Participation(EconomicAgentId agentId, ParticipationRole role)
    {
        if (agentId.Equals(EconomicAgentId.Empty))
            throw EconomicEventErrors.InvalidParticipationAgent();
        if (role is null)
            throw EconomicEventErrors.InvalidParticipationRole();

        AgentId = agentId;
        Role = role;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return AgentId;
        yield return Role;
    }
}
