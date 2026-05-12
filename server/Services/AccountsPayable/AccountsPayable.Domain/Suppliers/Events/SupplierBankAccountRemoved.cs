namespace AccountsPayable.Domain.Suppliers.Events;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Entities;

public sealed record SupplierBankAccountRemoved(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    SupplierId SupplierId,
    SupplierBankAccountId BankAccountId) : IDomainEvent;
