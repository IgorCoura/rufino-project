using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace PeopleManagement.Domain.SeedWord;

public interface IRepository<T> where T : Entity
{
    IUnitOfWork UnitOfWork { get; }
    Task<T> InsertAsync(T model, CancellationToken cancellation = default);
    Task DeleteAsync(T model);
    Task DeleteRangeAsync(IEnumerable<T> model);
    Task<T?> FirstOrDefaultAsync(Guid id, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, CancellationToken cancellation = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> filter, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, CancellationToken cancellation = default);
    Task<IEnumerable<T>> GetDataAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, int? skip = null, int? take = null, CancellationToken cancellation = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> filter);
    Task DebugChangeTracker();
}
