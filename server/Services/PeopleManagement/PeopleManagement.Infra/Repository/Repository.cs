using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using PeopleManagement.Domain.SeedWord;
using PeopleManagement.Infra.Context;
using System.Linq;
using System.Linq.Expressions;

namespace PeopleManagement.Infra.Repository
{
    public class Repository<T>(PeopleManagementContext context) : IRepository<T> where T : Entity
    {

        protected readonly PeopleManagementContext _context = context;

        public IUnitOfWork UnitOfWork => (IUnitOfWork)_context;

        public virtual async Task<T> InsertAsync(T model,  CancellationToken cancellation = default)
        {
            var result = await _context.Set<T>().AddAsync(model, cancellation);
            
            return result.Entity;
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

        public async Task<T?> FirstOrDefaultMemoryOrDatabase(Func<T, bool> filter)
        {
            var result =  _context.Set<T>().Local.FirstOrDefault(filter);
            result ??=  _context.Set<T>().FirstOrDefault(filter);
            return await Task.FromResult(result);
        }

        public async Task<T?> FirstOrDefaultAsync(Guid id, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, 
            CancellationToken cancellation = default)
        {
            var query = _context.Set<T>().AsQueryable();
            if (include != null)
                query = include(query);

            return await query.AsTracking().FirstOrDefaultAsync(x => x.Id == id, cancellation);
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> filter, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, CancellationToken cancellation = default)
        {
            var query = _context.Set<T>().AsQueryable();
            if (include != null)
                query = include(query);

            return await query.AsTracking().FirstOrDefaultAsync(filter, cancellation);
        }

        public async Task<IEnumerable<T>> GetDataAsync(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, int? skip = null, int? take = null, CancellationToken cancellation = default)
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

            return await query.AsTracking().ToListAsync(cancellation);
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> filter)
        {
            return await _context.Set<T>().AnyAsync(filter);
        }

        public Task DebugChangeTracker()
        {
            _context.ChangeTracker.DetectChanges();
            System.Diagnostics.Debug.WriteLine(_context.ChangeTracker.DebugView.LongView);
            return Task.CompletedTask;
        }
    }
}
