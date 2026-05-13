namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record PayableApprovalRequested(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId) : IDomainEvent;
