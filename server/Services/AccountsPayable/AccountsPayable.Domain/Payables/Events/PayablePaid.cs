namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record PayablePaid(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    PaymentOrderId PaymentOrderId,
    DateTime PaidAt) : IDomainEvent;
