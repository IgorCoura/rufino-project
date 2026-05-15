namespace AccountsPayable.Domain.ExpectedRecurringBills.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record ExpectedRecurringBillCancelled(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ExpectedRecurringBillId BillId,
    string Reason) : IDomainEvent;
