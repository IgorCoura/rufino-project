namespace EconomicCore.Infra.Repositories;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicEvents.ValueObjects;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class EconomicEventRepository : IEconomicEventRepository
{
    private readonly EconomicCoreDbContext _context;

    public EconomicEventRepository(EconomicCoreDbContext context) => _context = context;

    public IUnitOfWork UnitOfWork => _context;

    public async Task<EconomicEvent> InsertAsync(EconomicEvent economicEvent, CancellationToken cancellationToken = default)
    {
        var entry = await _context.EconomicEvents.AddAsync(economicEvent, cancellationToken);
        return entry.Entity;
    }

    public async Task<EconomicEvent?> GetByIdAsync(EconomicEventId id, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.EconomicEvents
            .FirstOrDefaultAsync(e => e.Id.Equals(id) && e.TenantId.Equals(tenantId), cancellationToken);
    }

    public async Task<EconomicEvent?> FindCoveredByCommitmentAsync(CommitmentId commitmentId, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.EconomicEvents
            .FirstOrDefaultAsync(e =>
                e.TenantId.Equals(tenantId)
                && e.CoveringCommitment != null
                && e.CoveringCommitment.CommitmentId.Equals(commitmentId)
                && e.Duality == null,
                cancellationToken);
    }

    public void Update(EconomicEvent economicEvent)
    {
        _context.EconomicEvents.Update(economicEvent);
    }
}
