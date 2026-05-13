namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record PayableRejected(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    UserId RejectedBy,
    string Reason) : IDomainEvent;
