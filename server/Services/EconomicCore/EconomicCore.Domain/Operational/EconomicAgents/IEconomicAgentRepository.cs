namespace EconomicCore.Domain.Operational.EconomicAgents;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public interface IEconomicAgentRepository
{
    IUnitOfWork UnitOfWork { get; }
    Task<EconomicAgent> InsertAsync(EconomicAgent agent, CancellationToken cancellationToken = default);
    Task<EconomicAgent?> GetByIdAsync(EconomicAgentId id, TenantId tenantId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(EconomicAgentId id, TenantId tenantId, CancellationToken cancellationToken = default);
}
