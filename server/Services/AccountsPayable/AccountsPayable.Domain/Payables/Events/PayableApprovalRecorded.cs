namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record PayableApprovalRecorded(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    UserId ApprovedBy,
    string Role) : IDomainEvent;
