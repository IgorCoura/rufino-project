namespace EconomicCore.Domain.SeedWork;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}
