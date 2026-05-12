namespace AccountsPayable.Domain.Services;

using AccountsPayable.Domain.Errors;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.ValueObjects;

/// <summary>
/// Domain Service that enforces the per-tenant uniqueness of <see cref="TaxId"/> on Supplier creation.
/// <para>
/// <b>Convention deviation</b>: this service is async because it depends on the
/// <see cref="ISupplierTaxIdLookup"/> port (port lives in Domain, implementation in Infra).
/// The skill <c>domain-codegen-ddd-dotnet</c> recommends sync-only Domain Services, but
/// <c>accounts-payable-sprints.md</c> explicitly designs this service with the port — sprint plan wins.
/// </para>
/// </summary>
public sealed class SupplierUniquenessChecker
{
    public async Task EnsureUniqueAsync(
        TenantId tenant,
        TaxId taxId,
        ISupplierTaxIdLookup lookup,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taxId);
        ArgumentNullException.ThrowIfNull(lookup);

        if (await lookup.ExistsAsync(tenant, taxId, cancellationToken).ConfigureAwait(false))
            throw SupplierUniquenessCheckerErrors.TaxIdAlreadyRegistered(taxId.Type.Name, taxId.MaskedValue);
    }
}
