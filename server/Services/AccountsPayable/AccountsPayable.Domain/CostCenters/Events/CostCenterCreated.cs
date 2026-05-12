namespace AccountsPayable.Domain.CostCenters.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record CostCenterCreated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    CostCenterId CostCenterId,
    string Code,
    string Name) : IDomainEvent;
