namespace AccountsPayable.Domain.InstallmentPlans.Events;

using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Cancelling the plan does <b>not</b> cancel the linked <see cref="Payable"/>s from inside the
/// <see cref="InstallmentPlan"/> — that would cross Aggregate boundaries. The Application layer
/// consumes this event and calls <c>Payable.Cancel</c> for each non-terminal payable in
/// <paramref name="LinkedPayableIds"/>; payables already in <c>Paid</c>/<c>Cancelled</c>/<c>Rejected</c>
/// are left untouched (critério de aceite Sprint 8).
/// </summary>
public sealed record InstallmentPlanCancelled(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    InstallmentPlanId InstallmentPlanId,
    string Reason,
    IReadOnlyList<PayableId> LinkedPayableIds) : IDomainEvent;
