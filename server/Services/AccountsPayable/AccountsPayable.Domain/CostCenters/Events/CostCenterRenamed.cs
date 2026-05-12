namespace AccountsPayable.Domain.CostCenters.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record CostCenterRenamed(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    CostCenterId CostCenterId,
    string OldName,
    string NewName) : IDomainEvent;
