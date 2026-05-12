namespace AccountsPayable.Domain.Services;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.ValueObjects;

/// <summary>
/// Domain port: tells whether a Supplier with the given <see cref="TaxId"/> already exists
/// for a tenant. Implementations live in Infrastructure (typically backed by EF Core).
/// </summary>
public interface ISupplierTaxIdLookup
{
    Task<bool> ExistsAsync(TenantId tenant, TaxId taxId, CancellationToken cancellationToken = default);
}
