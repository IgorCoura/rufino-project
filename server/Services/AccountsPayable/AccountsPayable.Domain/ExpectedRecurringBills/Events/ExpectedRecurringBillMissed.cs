namespace AccountsPayable.Domain.ExpectedRecurringBills.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record ExpectedRecurringBillMissed(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ExpectedRecurringBillId BillId) : IDomainEvent;
