namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Multi-approver variant of <see cref="PayableApprovalRequested"/> (Sprint 10). Replaces the
/// single-approver default flow when the <c>ApprovalRequirementCalculator</c> returns more than
/// one required approval.
/// </summary>
public sealed record PayableMultiApprovalRequested(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    int RequiredApprovalCount,
    IReadOnlyList<string> EligibleApproverRoles) : IDomainEvent;
