namespace EconomicCore.Domain.Operational.EconomicResources;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicResources.Enumerations;
using EconomicCore.Domain.Operational.EconomicResources.Events;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public sealed class EconomicResource : AggregateRoot<EconomicResourceId>
{
    public const int NAME_MAX_LENGTH = 200;

    public TenantId TenantId { get; private set; }
    public EconomicResourceTypeId? TypeId { get; private set; }
    public ResourceKind Kind { get; private set; } = default!;
    public string Name { get; private set; } = string.Empty;
    public EconomicAgentId? CustodianId { get; private set; }

    private EconomicResource() : base() { }
    private EconomicResource(EconomicResourceId id) : base(id) { }

    public static EconomicResource Create(
        EconomicResourceId id,
        TenantId tenantId,
        string name,
        ResourceKind kind,
        EconomicResourceTypeId? typeId,
        EconomicAgentId? custodianId,
        DateTime occurredAt)
    {
        var resource = new EconomicResource(id)
        {
            TenantId = tenantId,
            TypeId = typeId,
            CustodianId = custodianId,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };
        resource.SetName(name);
        resource.SetKind(kind);

        resource.AddDomainEvent(new EconomicResourceRegistered(
            EventId: Guid.NewGuid(),
            ResourceId: resource.Id,
            TenantId: resource.TenantId,
            Name: resource.Name,
            KindName: resource.Kind.Name,
            TypeId: resource.TypeId?.Value,
            CustodianId: resource.CustodianId?.Value,
            OccurredAt: occurredAt));

        return resource;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > NAME_MAX_LENGTH)
            throw EconomicResourceErrors.InvalidName(name ?? string.Empty, NAME_MAX_LENGTH);

        Name = name;
    }

    private void SetKind(ResourceKind kind)
    {
        if (kind is null)
            throw EconomicResourceErrors.MissingKind();

        Kind = kind;
    }
}
