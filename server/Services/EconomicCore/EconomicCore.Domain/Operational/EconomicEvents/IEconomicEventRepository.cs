namespace EconomicCore.Domain.Operational.EconomicEvents;

using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;
using EconomicCore.Domain.SharedKernel;

public interface IEconomicEventRepository
{
    IUnitOfWork UnitOfWork { get; }
    Task<EconomicEvent> InsertAsync(EconomicEvent economicEvent, CancellationToken cancellationToken = default);
    Task<EconomicEvent?> GetByIdAsync(EconomicEventId id, TenantId tenantId, CancellationToken cancellationToken = default);
    Task<EconomicEvent?> FindCoveredByCommitmentAsync(CommitmentId commitmentId, TenantId tenantId, CancellationToken cancellationToken = default);
    void Update(EconomicEvent economicEvent);
}
