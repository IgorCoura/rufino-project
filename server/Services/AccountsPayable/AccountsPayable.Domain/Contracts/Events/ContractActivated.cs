namespace AccountsPayable.Domain.Contracts.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record ContractActivated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ContractId ContractId) : IDomainEvent;
