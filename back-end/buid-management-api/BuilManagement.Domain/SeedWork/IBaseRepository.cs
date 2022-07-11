using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;


namespace BuildManagement.Domain.SeedWork
{
    public interface IBaseRepository<T> where T : Entity
    {
        IUnitOfWork UnitOfWork { get; }
        Task<T> RegisterAsync(T model, CancellationToken cancellationToken = default(CancellationToken));
        Task<T> UpdateAsync(T model, CancellationToken cancellationToken = default(CancellationToken));
        Task DeleteAsync(T model, CancellationToken cancellationToken = default(CancellationToken));
        Task<T?> FirstAsync(Expression<Func<T, bool>> filter, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, CancellationToken cancellationToken = default(CancellationToken));
        Task<T?> FirstAsyncAsTracking(Expression<Func<T, bool>> filter, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, CancellationToken cancellationToken = default(CancellationToken));
        Task<IEnumerable<T>> GetDataAsync(
            Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>,
            IIncludableQueryable<T, object>>? include = null,
            int? skip = null, int? take = null, 
            CancellationToken cancellationToken = default(CancellationToken));
        TResult QueryData<TResult>(Func<IQueryable<T>, TResult> queryParm, Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default(CancellationToken));
        Task<bool> HasAnyAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default(CancellationToken));
    }
}
