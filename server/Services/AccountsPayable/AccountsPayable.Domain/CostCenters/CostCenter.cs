namespace AccountsPayable.Domain.CostCenters;

using AccountsPayable.Domain.CostCenters.Events;
using AccountsPayable.Domain.CostCenters.ValueObjects;
using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Aggregate Root representing a cost center (centro de custo). Snapshot-persisted, no inner
/// entities. Code uniqueness per tenant is enforced at the infra layer (database unique index),
/// not here — a single CostCenter aggregate has no awareness of its siblings.
/// </summary>
public sealed class CostCenter : AggregateRoot<CostCenterId>
{
    public TenantId TenantId { get; private set; }
    public CostCenterCode Code { get; private set; } = default!;
    public CostCenterName Name { get; private set; } = default!;
    public bool IsActive { get; private set; }

    private CostCenter() : base() { }

    private CostCenter(CostCenterId id) : base(id) { }

    public static CostCenter Create(
        CostCenterId id,
        TenantId tenantId,
        CostCenterCode code,
        CostCenterName name,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(name);

        var center = new CostCenter(id)
        {
            TenantId = tenantId,
            Code = code,
            Name = name,
            IsActive = true,
            CreatedAt = occurredAt,
            UpdatedAt = occurredAt,
        };

        center.AddDomainEvent(new CostCenterCreated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: tenantId,
            CostCenterId: id,
            Code: code.Value,
            Name: name.Value));

        return center;
    }

    public void Rename(CostCenterName newName, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(newName);
        if (Name.Equals(newName))
            return; // idempotent

        var oldName = Name;
        Name = newName;
        UpdatedAt = occurredAt;

        AddDomainEvent(new CostCenterRenamed(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            CostCenterId: Id,
            OldName: oldName.Value,
            NewName: newName.Value));
    }

    public void Deactivate(DateTime occurredAt)
    {
        if (!IsActive)
            throw CostCenterErrors.AlreadyInactive();

        IsActive = false;
        UpdatedAt = occurredAt;

        AddDomainEvent(new CostCenterDeactivated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            CostCenterId: Id));
    }

    public void Reactivate(DateTime occurredAt)
    {
        if (IsActive)
            throw CostCenterErrors.AlreadyActive();

        IsActive = true;
        UpdatedAt = occurredAt;

        AddDomainEvent(new CostCenterReactivated(
            EventId: Guid.NewGuid(),
            OccurredAt: occurredAt,
            TenantId: TenantId,
            CostCenterId: Id));
    }
}
