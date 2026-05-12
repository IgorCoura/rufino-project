namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record PayableMarkedAsPaid(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    DateTime PaidAt,
    string ProofUri,
    string ProofType) : IDomainEvent;
