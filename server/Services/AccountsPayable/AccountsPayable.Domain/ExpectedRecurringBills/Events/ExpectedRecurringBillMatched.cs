namespace AccountsPayable.Domain.ExpectedRecurringBills.Events;

using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.SeedWork;

public sealed record ExpectedRecurringBillMatched(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ExpectedRecurringBillId BillId,
    PayableId MatchedPayableId) : IDomainEvent;
