namespace EconomicCore.Infra.Repositories;

using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class EconomicContractRepository : IEconomicContractRepository
{
    private readonly EconomicCoreDbContext _context;

    public EconomicContractRepository(EconomicCoreDbContext context) => _context = context;

    public IUnitOfWork UnitOfWork => _context;

    public async Task<EconomicContract> InsertAsync(EconomicContract contract, CancellationToken cancellationToken = default)
    {
        var entry = await _context.EconomicContracts.AddAsync(contract, cancellationToken);
        return entry.Entity;
    }

    public async Task<EconomicContract?> GetByIdAsync(EconomicContractId id, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.EconomicContracts
            .Include(c => c.Commitments)
            .FirstOrDefaultAsync(e => e.Id.Equals(id) && e.TenantId.Equals(tenantId), cancellationToken);
    }

    public async Task<EconomicContract?> FindByCommitmentIdAsync(CommitmentId commitmentId, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.EconomicContracts
            .Include(c => c.Commitments)
            .FirstOrDefaultAsync(
                c => c.TenantId.Equals(tenantId) && c.Commitments.Any(cm => cm.Id.Equals(commitmentId)),
                cancellationToken);
    }

    public void Update(EconomicContract contract)
    {
        _context.EconomicContracts.Update(contract);
    }

    public async Task<bool> HasOverlappingAsync(
        EconomicResourceId resourceId,
        DateOnly startDate,
        int termMonths,
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        var newEnd = startDate.AddMonths(termMonths);

        var candidates = await _context.EconomicContracts
            .AsNoTracking()
            .Where(c => c.TenantId.Equals(tenantId)
                && c.ResourceId.Equals(resourceId)
                && (c.Status == ContractStatus.Draft || c.Status == ContractStatus.Active)
                && c.StartDate < newEnd)
            .Select(c => new { c.StartDate, c.TermMonths })
            .ToListAsync(cancellationToken);

        return candidates.Any(c => c.StartDate.AddMonths(c.TermMonths) > startDate);
    }
}
