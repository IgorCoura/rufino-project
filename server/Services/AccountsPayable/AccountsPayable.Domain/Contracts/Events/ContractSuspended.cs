namespace AccountsPayable.Domain.Contracts.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record ContractSuspended(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ContractId ContractId,
    string Reason) : IDomainEvent;
