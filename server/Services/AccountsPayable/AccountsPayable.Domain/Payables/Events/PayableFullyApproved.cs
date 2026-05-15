namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Emitted right after the last <see cref="PayableApprovalRecorded"/> that satisfies
/// <c>RequiredApprovalCount</c> (Sprint 10). Transitions status to
/// <see cref="Enumerations.PayableStatus.Approved"/>.
/// </summary>
public sealed record PayableFullyApproved(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId) : IDomainEvent;
