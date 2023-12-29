using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace PeopleManagement.Domain.SeedWord;

public interface IRepository<T> where T : Entity
{
    IUnitOfWork UnitOfWork { get; }
    Task<T> InsertAsync(T model);
    Task<T> UpdateAsync(T model);
    Task DeleteAsync(T model);
    Task DeleteRangeAsync(IEnumerable<T> model);
    Task<T?> FirstAsync(Expression<Func<T, bool>> filter, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, CancellationToken cancellation = default);
}
