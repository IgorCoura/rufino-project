namespace EconomicCore.Infra.Repositories;

using EconomicCore.Domain.Operational.EconomicEvents;
using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
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

    public async Task<CompetencePeriod?> GetLastInflowPeriodForCommitmentsAsync(
        IReadOnlyCollection<CommitmentId> commitmentIds,
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        if (commitmentIds.Count == 0)
            return null;

        var ids = commitmentIds.Select(c => c.Value).ToHashSet();

        var candidates = await _context.EconomicEvents
            .AsNoTracking()
            .Where(e => e.TenantId.Equals(tenantId)
                && e.Direction == FlowDirection.Inflow
                && e.CoveringCommitment != null)
            .Select(e => new
            {
                CommitmentId = e.CoveringCommitment!.CommitmentId.Value,
                e.Competence.Year,
                e.Competence.Month,
            })
            .ToListAsync(cancellationToken);

        var top = candidates
            .Where(e => ids.Contains(e.CommitmentId))
            .OrderByDescending(e => e.Year)
            .ThenByDescending(e => e.Month)
            .FirstOrDefault();

        return top is null ? null : new CompetencePeriod(top.Year, top.Month);
    }
}
