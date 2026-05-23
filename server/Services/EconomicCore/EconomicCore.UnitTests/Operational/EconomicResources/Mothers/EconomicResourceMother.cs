namespace EconomicCore.UnitTests.Operational.EconomicResources.Mothers;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Operational.EconomicResources.Enumerations;
using EconomicCore.Domain.SharedKernel;

public sealed class EconomicResourceMother
{
    public static readonly DateTime FixedOccurredAt = new(2025, 10, 1, 10, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId FixedTenantId = TenantId.From(new Guid("11111111-1111-7111-8111-111111111111"));
    public static readonly EconomicResourceId FixedResourceId = EconomicResourceId.From(new Guid("33333333-3333-7333-8333-333333333333"));
    public const string DEFAULT_NAME = "Caixa Principal";

    private EconomicResourceId _id = FixedResourceId;
    private TenantId _tenantId = FixedTenantId;
    private string _name = DEFAULT_NAME;
    private ResourceKind _kind = ResourceKind.Cash;
    private EconomicResourceTypeId? _typeId;
    private EconomicAgentId? _custodianId;
    private DateTime _occurredAt = FixedOccurredAt;

    public static EconomicResourceMother New() => new();

    public EconomicResourceMother WithId(EconomicResourceId id)
    {
        _id = id;
        return this;
    }

    public EconomicResourceMother WithTenant(TenantId tenantId)
    {
        _tenantId = tenantId;
        return this;
    }

    public EconomicResourceMother WithName(string name)
    {
        _name = name;
        return this;
    }

    public EconomicResourceMother WithKind(ResourceKind kind)
    {
        _kind = kind;
        return this;
    }

    public EconomicResourceMother WithTypeId(EconomicResourceTypeId typeId)
    {
        _typeId = typeId;
        return this;
    }

    public EconomicResourceMother WithCustodian(EconomicAgentId custodianId)
    {
        _custodianId = custodianId;
        return this;
    }

    public EconomicResourceMother WithoutCustodian()
    {
        _custodianId = null;
        return this;
    }

    public EconomicResourceMother At(DateTime occurredAt)
    {
        _occurredAt = occurredAt;
        return this;
    }

    public EconomicResource Build()
        => EconomicResource.Create(_id, _tenantId, _name, _kind, _typeId, _custodianId, _occurredAt);
}
