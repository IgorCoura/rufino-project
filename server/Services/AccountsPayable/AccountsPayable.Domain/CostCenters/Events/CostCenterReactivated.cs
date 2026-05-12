namespace AccountsPayable.Domain.CostCenters.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record CostCenterReactivated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    CostCenterId CostCenterId) : IDomainEvent;
