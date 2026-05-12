namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record PayableScheduled(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    DateOnly ScheduledFor) : IDomainEvent;
