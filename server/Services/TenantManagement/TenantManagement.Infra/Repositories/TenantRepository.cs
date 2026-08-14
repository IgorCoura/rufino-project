namespace TenantManagement.Infra.Repositories;

using TenantManagement.Domain.SharedKernel;
using TenantManagement.Domain.Tenants;
using TenantManagement.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class TenantRepository(TenantManagementDbContext context) : ITenantRepository
{
    public async Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
        => await context.Tenants.AddAsync(tenant, cancellationToken);

    // Sem Include: produtos e vínculos são owned collections e o EF os carrega com o agregado.
    public Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken = default)
        => context.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(TenantId id, CancellationToken cancellationToken = default)
        => context.Tenants.AsNoTracking().AnyAsync(t => t.Id == id, cancellationToken);

    public Task<bool> ExistsByPrimaryTaxIdAsync(TaxId taxId, CancellationToken cancellationToken = default)
        => context.Tenants.AsNoTracking().AnyAsync(t => t.PrimaryTaxId == taxId, cancellationToken);
}
