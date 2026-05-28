namespace EconomicCore.Infra.Repositories;

using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class EconomicResourceRepository : IEconomicResourceRepository
{
    private readonly EconomicCoreDbContext _context;

    public EconomicResourceRepository(EconomicCoreDbContext context) => _context = context;

    public IUnitOfWork UnitOfWork => _context;

    public async Task<EconomicResource> InsertAsync(EconomicResource resource, CancellationToken cancellationToken = default)
    {
        var entry = await _context.EconomicResources.AddAsync(resource, cancellationToken);
        return entry.Entity;
    }

    public async Task<EconomicResource?> GetByIdAsync(EconomicResourceId id, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.EconomicResources
            .FirstOrDefaultAsync(e => e.Id.Equals(id) && e.TenantId.Equals(tenantId), cancellationToken);
    }

    public async Task<bool> ExistsAsync(EconomicResourceId id, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.EconomicResources
            .AnyAsync(e => e.Id.Equals(id) && e.TenantId.Equals(tenantId), cancellationToken);
    }
}
