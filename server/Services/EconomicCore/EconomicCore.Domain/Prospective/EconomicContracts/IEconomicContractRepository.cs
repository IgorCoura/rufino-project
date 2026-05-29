namespace EconomicCore.Domain.Prospective.EconomicContracts;

using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public interface IEconomicContractRepository
{
    IUnitOfWork UnitOfWork { get; }
    Task<EconomicContract> InsertAsync(EconomicContract contract, CancellationToken cancellationToken = default);
    Task<EconomicContract?> GetByIdAsync(EconomicContractId id, TenantId tenantId, CancellationToken cancellationToken = default);
    Task<EconomicContract?> FindByCommitmentIdAsync(CommitmentId commitmentId, TenantId tenantId, CancellationToken cancellationToken = default);
    void Update(EconomicContract contract);

    Task<bool> HasOverlappingAsync(
        EconomicCore.Domain.Operational.EconomicResources.EconomicResourceId resourceId,
        DateOnly startDate,
        int termMonths,
        TenantId tenantId,
        CancellationToken cancellationToken = default);
}
