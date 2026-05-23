namespace EconomicCore.UnitTests.Operational.EconomicAgents.Mothers;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicAgents.Enumerations;
using EconomicCore.Domain.SharedKernel;

public sealed class EconomicAgentMother
{
    public static readonly DateTime FixedOccurredAt = new(2025, 10, 1, 10, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId FixedTenantId = TenantId.From(new Guid("11111111-1111-7111-8111-111111111111"));
    public static readonly EconomicAgentId FixedAgentId = EconomicAgentId.From(new Guid("22222222-2222-7222-8222-222222222222"));
    public const string DEFAULT_NAME = "Test Agent";

    private EconomicAgentId _id = FixedAgentId;
    private TenantId _tenantId = FixedTenantId;
    private string _name = DEFAULT_NAME;
    private AgentScope _scope = AgentScope.Outside;
    private TaxId? _taxId;
    private DateTime _occurredAt = FixedOccurredAt;

    public static EconomicAgentMother New() => new();

    public EconomicAgentMother WithId(EconomicAgentId id)
    {
        _id = id;
        return this;
    }

    public EconomicAgentMother WithTenant(TenantId tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public EconomicAgentMother WithName(string name)
    {
        _name = name;
        return this;
    }

    public EconomicAgentMother WithScope(AgentScope scope)
    {
        _scope = scope;
        return this;
    }

    public EconomicAgentMother WithTaxId(TaxId taxId)
    {
        _taxId = taxId;
        return this;
    }

    public EconomicAgentMother WithoutTaxId()
    {
        _taxId = null;
        return this;
    }

    public EconomicAgentMother At(DateTime occurredAt)
    {
        _occurredAt = occurredAt;
        return this;
    }

    public EconomicAgent Build()
        => EconomicAgent.Create(_id, _tenantId, _name, _scope, _taxId, _occurredAt);
}
