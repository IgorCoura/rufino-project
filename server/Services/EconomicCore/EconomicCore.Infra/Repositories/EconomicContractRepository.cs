namespace EconomicCore.Infra.Repositories;

using EconomicCore.Domain.Prospective.EconomicContracts;
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

    public void Update(EconomicContract contract)
    {
        _context.EconomicContracts.Update(contract);
    }
}
