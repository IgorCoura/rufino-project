namespace EconomicCore.Domain.Operational.EconomicResources;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public interface IEconomicResourceRepository
{
    IUnitOfWork UnitOfWork { get; }
    Task<EconomicResource> InsertAsync(EconomicResource resource, CancellationToken cancellationToken = default);
    Task<EconomicResource?> GetByIdAsync(EconomicResourceId id, TenantId tenantId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(EconomicResourceId id, TenantId tenantId, CancellationToken cancellationToken = default);
}
