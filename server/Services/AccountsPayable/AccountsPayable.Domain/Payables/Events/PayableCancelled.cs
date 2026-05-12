namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record PayableCancelled(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    string Reason) : IDomainEvent;
