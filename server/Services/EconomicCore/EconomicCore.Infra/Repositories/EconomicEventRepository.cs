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
        // Tenant-filtered in SQL; the allocation/duality predicates run in memory because they reach into owned
        // collections (and a nullable converted VO on the link) that EF does not translate reliably. Owned
        // collections load eagerly with the aggregate, so Allocations/DualityLinks are populated here.
        var candidates = await _context.EconomicEvents
            .Where(e => e.TenantId.Equals(tenantId))
            .ToListAsync(cancellationToken);

        return candidates.FirstOrDefault(e =>
            e.Allocations.Any(a => a.Commitment.CommitmentId.Equals(commitmentId))
            && !e.DualityLinks.Any(d => d.CommitmentId is { } c && c.Equals(commitmentId)));
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

        var inflowEvents = await _context.EconomicEvents
            .AsNoTracking()
            .Where(e => e.TenantId.Equals(tenantId) && e.Direction == FlowDirection.Inflow)
            .ToListAsync(cancellationToken);

        var top = inflowEvents
            .SelectMany(e => e.Allocations.Select(a => new
            {
                CommitmentId = a.Commitment.CommitmentId.Value,
                e.Competence.Year,
                e.Competence.Month,
            }))
            .Where(e => ids.Contains(e.CommitmentId))
            .OrderByDescending(e => e.Year)
            .ThenByDescending(e => e.Month)
            .FirstOrDefault();

        return top is null ? null : new CompetencePeriod(top.Year, top.Month);
    }
}
