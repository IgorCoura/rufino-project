namespace EconomicCore.Domain.Operational.EconomicAgents;

using EconomicCore.Domain.Operational.EconomicAgents.Enumerations;
using EconomicCore.Domain.Operational.EconomicAgents.Events;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed class EconomicAgent : AggregateRoot<EconomicAgentId>
{
    public const int NAME_MAX_LENGTH = 200;

    private readonly List<EconomicRoleId> _roles = [];

    public TenantId TenantId { get; private set; }
    public AgentScope Scope { get; private set; } = default!;
    public string Name { get; private set; } = string.Empty;
    public TaxId? TaxId { get; private set; }
    public IReadOnlyCollection<EconomicRoleId> Roles => _roles.AsReadOnly();

    private EconomicAgent() : base() { }
    private EconomicAgent(EconomicAgentId id) : base(id) { }

    public static EconomicAgent Create(
        EconomicAgentId id,
        TenantId tenantId,
        string name,
        AgentScope scope,
        TaxId? taxId,
        DateTime occurredAt)
    {
        var agent = new EconomicAgent(id)
        {
            TenantId = tenantId,
            TaxId = taxId,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };
        agent.SetName(name);
        agent.SetScope(scope);

        agent.AddDomainEvent(new EconomicAgentRegistered(
            EventId: Guid.NewGuid(),
            AgentId: agent.Id,
            TenantId: agent.TenantId,
            Name: agent.Name,
            ScopeName: agent.Scope.Name,
            TaxIdValue: agent.TaxId?.Value,
            TaxIdKindName: agent.TaxId?.Kind.Name,
            OccurredAt: occurredAt));

        return agent;
    }

    /// <summary>
    /// Factory from primitive tax-id inputs: composes the optional TaxId VO internally so callers
    /// (Application) never assemble domain types. A blank value or a missing kind yields no TaxId.
    /// </summary>
    public static EconomicAgent Create(
        EconomicAgentId id,
        TenantId tenantId,
        string name,
        AgentScope scope,
        string? taxIdValue,
        TaxIdKind? taxIdKind,
        DateTime occurredAt)
    {
        var taxId = !string.IsNullOrWhiteSpace(taxIdValue) && taxIdKind is not null
            ? new TaxId(taxIdValue, taxIdKind)
            : null;

        return Create(id, tenantId, name, scope, taxId, occurredAt);
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > NAME_MAX_LENGTH)
            throw EconomicAgentErrors.InvalidName(name ?? string.Empty, NAME_MAX_LENGTH);

        Name = name;
    }

    private void SetScope(AgentScope scope)
    {
        if (scope is null)
            throw EconomicAgentErrors.MissingScope();

        Scope = scope;
    }
}
