namespace AccountsPayable.UnitTests.Services._TestDoubles;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Services;
using AccountsPayable.Domain.Suppliers.ValueObjects;

internal sealed class FakeSupplierTaxIdLookup : ISupplierTaxIdLookup
{
    private readonly HashSet<(Guid Tenant, string TaxId)> _registry = new();

    public int Calls { get; private set; }

    public void Register(TenantId tenant, TaxId taxId)
        => _registry.Add((tenant.Value, taxId.Value));

    public Task<bool> ExistsAsync(TenantId tenant, TaxId taxId, CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(_registry.Contains((tenant.Value, taxId.Value)));
    }
}
