using Commom.Domain.SeedWork;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Commom.Infra.Base
{
    public class BaseRepository<T> : IBaseRepository<T> where T : Entity
    {

        protected readonly BaseContext _context;

        public BaseRepository(BaseContext context)
        {
            _context = context;
        }

        public IUnitOfWork UnitOfWork => (IUnitOfWork)_context;

        public virtual async Task<T> RegisterAsync(T model)
        {
            var result = _context.Set<T>().Add(model).Entity;
            return await Task.FromResult(result);
        }

        public virtual async Task<T> UpdateAsync(T model)
        {
            _context.Entry<T>(model).State = EntityState.Modified;
            return await Task.FromResult(model);
        }

        public virtual Task DeleteAsync(T model)
        {
            _context.Set<T>().Remove(model);           
            return Task.CompletedTask;
        }

        public virtual Task DeleteRangeAsync(IEnumerable<T> model) 
        {
            _context.Set<T>().RemoveRange(model);
            return Task.CompletedTask;
        }

        public async Task<T?> FirstAsync(Expression<Func<T, bool>> filter, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
        {
            var query = _context.Set<T>().AsQueryable();
            if (include != null)
                query = include(query);

            return await query.FirstOrDefaultAsync(filter);
        }

        public async Task<T?> FirstAsyncAsTracking(Expression<Func<T, bool>> filter, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null)
        {
            var query = _context.Set<T>().AsQueryable();
            if (include != null)
                query = include(query);

            return await query.AsTracking().FirstOrDefaultAsync(filter);
        }

        public async Task<IEnumerable<T>> GetDataAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, int? skip = null, int? take = null)
        {
            var query = _context.Set<T>().AsQueryable();
            if (filter != null)
                query = query.Where(filter);

            if (include != null)
                query = include(query);

            if (skip != null && skip.HasValue)
                query = query.Skip(skip.Value);

            if (take != null && take.HasValue)
                query = query.Take(take.Value);

            return await query.ToListAsync();
        }

        public TResult QueryData<TResult>(Func<IQueryable<T>, TResult> queryParm, Expression<Func<T, bool>>? filter = null)
        {
            var query = _context.Set<T>().AsQueryable<T>();
            if (filter != null)
                query = query.Where(filter);

            var result = queryParm(query);
            return result;
        }

        public async Task<bool> HasAnyAsync(Expression<Func<T, bool>> filter)
        {
            var result = await _context.Set<T>().AnyAsync(filter);
            return result;
        }
    }
}
