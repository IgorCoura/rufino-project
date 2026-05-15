namespace AccountsPayable.Domain.Contracts.Events;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Carries the cascade signal for the Application layer to cancel non-matched future
/// <c>ExpectedRecurringBill</c>s linked to this contract (Sprint 11 critério de aceite). The
/// Contract Aggregate doesn't reach into ERB aggregates itself — handler responsibility.
/// </summary>
public sealed record ContractTerminated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ContractId ContractId,
    string Reason) : IDomainEvent;
