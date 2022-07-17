using BuildManagement.Domain.SeedWork;
using BuildManagement.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Repository
{
    public abstract class BaseRepository<T> : IBaseRepository<T> where T : Entity
    {
     
        protected readonly ApplicationContext _context;

        public BaseRepository(ApplicationContext context)
        {
            _context = context;
        }

        public IUnitOfWork UnitOfWork => (IUnitOfWork)_context;

        public virtual async Task<T> RegisterAsync(T model, CancellationToken cancellationToken = default(CancellationToken))
        {

            var result = _context.Set<T>().Add(model).Entity;
            return await Task.FromResult(result);
        }

        public virtual async Task<T> UpdateAsync(T model, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = _context.Set<T>().Update(model).Entity;
            return await Task.FromResult(result);
        }

        public virtual Task DeleteAsync(T model, CancellationToken cancellationToken = default(CancellationToken))
        {
            _context.Set<T>().Remove(model);
            return Task.CompletedTask;
        }

        public async Task<T?> FirstAsync(Expression<Func<T, bool>> filter, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = _context.Set<T>().AsQueryable();
            if (include != null)
                query = include(query);

            return await query.FirstOrDefaultAsync(filter);
        }

        public async Task<T?> FirstAsyncAsTracking(Expression<Func<T, bool>> filter, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = _context.Set<T>().AsQueryable();
            if (include != null)
                query = include(query);

            return await query.AsTracking().FirstOrDefaultAsync(filter);
        }

        public async Task<IEnumerable<T>> GetDataAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default(CancellationToken))
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

        public TResult QueryData<TResult>(Func<IQueryable<T>, TResult> queryParm, Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = _context.Set<T>().AsQueryable<T>();
            if (filter != null)
                query = query.Where(filter);

            var result = queryParm(query);
            return result;
        }

        public async Task<bool> HasAnyAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await _context.Set<T>().AnyAsync(filter, cancellationToken);
            return result;
        }
    }
 
}
