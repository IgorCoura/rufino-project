namespace AccountsPayable.Domain.Suppliers.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record SupplierReactivated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    SupplierId SupplierId) : IDomainEvent;
