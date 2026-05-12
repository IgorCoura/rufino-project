namespace AccountsPayable.Domain.Suppliers.Events;

using AccountsPayable.Domain.SeedWork;

public sealed record SupplierContactUpdated(
    Guid EventId,
    DateTime OccurredAt,
    TenantId TenantId,
    SupplierId SupplierId,
    string Email,
    string? Phone) : IDomainEvent;
