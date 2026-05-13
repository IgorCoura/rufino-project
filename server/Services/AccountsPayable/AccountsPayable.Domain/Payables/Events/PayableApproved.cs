namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record PayableApproved(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    UserId ApprovedBy) : IDomainEvent;
