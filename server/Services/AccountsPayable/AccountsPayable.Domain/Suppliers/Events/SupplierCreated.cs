namespace AccountsPayable.Domain.Suppliers.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record SupplierCreated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    SupplierId SupplierId,
    string LegalName,
    string? TradeName,
    string TaxIdValue,
    string TaxIdType) : IDomainEvent;
