using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using PeopleManagement.Domain.SeedWord;
using PeopleManagement.Infra.Context;
using System.Linq.Expressions;

namespace PeopleManagement.Infra.Repository
{
    public class Repository<T> : IRepository<T> where T : Entity
    {

        protected readonly PeopleManagementContext _context;

        public Repository(PeopleManagementContext context)
        {
            _context = context;
        }

        public IUnitOfWork UnitOfWork => (IUnitOfWork)_context;

        public virtual async Task<T> InsertAsync(T model)
        {
            var result = _context.Set<T>().Add(model).Entity;
            return await Task.FromResult(result);
        }

        public virtual async Task<T> UpdateAsync(T model)
        {
            _context.Set<T>().Update(model);
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

        public async Task<T?> FirstAsync(Expression<Func<T, bool>> filter, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, CancellationToken cancellation = default)
        {
            var query = _context.Set<T>().AsQueryable();
            if (include != null)
                query = include(query);

            return await query.FirstOrDefaultAsync(filter, cancellation);
        }
    }
}
