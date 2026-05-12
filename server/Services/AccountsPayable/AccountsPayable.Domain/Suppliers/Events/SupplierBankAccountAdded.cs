namespace AccountsPayable.Domain.Suppliers.Events;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Entities;

public sealed record SupplierBankAccountAdded(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    SupplierId SupplierId,
    SupplierBankAccountId BankAccountId,
    string BankCode,
    string Branch,
    string AccountNumber,
    string AccountType) : IDomainEvent;
