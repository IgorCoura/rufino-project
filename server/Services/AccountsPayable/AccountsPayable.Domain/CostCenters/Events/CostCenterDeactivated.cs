namespace AccountsPayable.Domain.CostCenters.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record CostCenterDeactivated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    CostCenterId CostCenterId) : IDomainEvent;
