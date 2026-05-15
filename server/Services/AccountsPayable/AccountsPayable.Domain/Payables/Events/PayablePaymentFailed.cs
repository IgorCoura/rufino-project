namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record PayablePaymentFailed(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    PaymentOrderId PaymentOrderId,
    string Reason) : IDomainEvent;
