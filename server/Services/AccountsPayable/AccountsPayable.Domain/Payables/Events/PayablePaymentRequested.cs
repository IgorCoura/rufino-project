namespace AccountsPayable.Domain.Payables.Events;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;
using AccountsPayable.Domain.Suppliers.Entities;

public sealed record PayablePaymentRequested(
    Guid EventId,
    DateTime OccurredAt,
    PayableId PayableId,
    SupplierId SupplierId,
    decimal AmountValue,
    string AmountCurrency,
    SupplierBankAccountId BankAccountId,
    string Method) : IDomainEvent;
