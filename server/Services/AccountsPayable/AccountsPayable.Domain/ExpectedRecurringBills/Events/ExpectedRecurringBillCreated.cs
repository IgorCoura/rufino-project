namespace AccountsPayable.Domain.ExpectedRecurringBills.Events;

using AccountsPayable.Domain.Contracts;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

public sealed record ExpectedRecurringBillCreated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    ExpectedRecurringBillId BillId,
    ContractId ContractId,
    SupplierId SupplierId,
    DateOnly ExpectedDueDate,
    decimal ExpectedAmountValue,
    string ExpectedAmountCurrency) : IDomainEvent;
