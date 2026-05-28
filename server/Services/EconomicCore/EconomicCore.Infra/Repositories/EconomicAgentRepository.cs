namespace EconomicCore.Infra.Repositories;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class EconomicAgentRepository : IEconomicAgentRepository
{
    private readonly EconomicCoreDbContext _context;

    public EconomicAgentRepository(EconomicCoreDbContext context) => _context = context;

    public IUnitOfWork UnitOfWork => _context;

    public async Task<EconomicAgent> InsertAsync(EconomicAgent agent, CancellationToken cancellationToken = default)
    {
        var entry = await _context.EconomicAgents.AddAsync(agent, cancellationToken);
        return entry.Entity;
    }

    public async Task<EconomicAgent?> GetByIdAsync(EconomicAgentId id, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.EconomicAgents
            .FirstOrDefaultAsync(e => e.Id.Equals(id) && e.TenantId.Equals(tenantId), cancellationToken);
    }

    public async Task<bool> ExistsAsync(EconomicAgentId id, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.EconomicAgents
            .AnyAsync(e => e.Id.Equals(id) && e.TenantId.Equals(tenantId), cancellationToken);
    }
}
