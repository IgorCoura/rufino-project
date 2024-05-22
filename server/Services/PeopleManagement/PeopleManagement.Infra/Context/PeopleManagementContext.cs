using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.SeedWord;
using PeopleManagement.Infra.Extension;
using PeopleManagement.Infra.Mapping;
using System.Data;

namespace PeopleManagement.Infra.Context
{
    public class PeopleManagementContext : DbContext, IUnitOfWork
    {
        public const string DEFAULT_SCHEMA = "people_management";

        public DbSet<Company> Companies { get; set; }

        private readonly IMediator? _mediator;
        private readonly IDbContextTransaction? _currentTransaction;

        public IDbContextTransaction? GetCurrentTransaction() => _currentTransaction;
        public bool HasActiveTransaction => _currentTransaction != null;

        public PeopleManagementContext(DbContextOptions<PeopleManagementContext> options) : base(options)
        {

        }

        public PeopleManagementContext(DbContextOptions<PeopleManagementContext> options, IMediator? mediator) : base(options)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new CompanyMap());
        }

        public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
        {
            if (_mediator != null)
            {
                await _mediator.DispatchDomainEventsAsync(this);
            }

            await UpdateDatetimeEntities();

            await base.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {

            if (_mediator != null)
            {
                await _mediator.DispatchDomainEventsAsync(this);
            }

            await UpdateDatetimeEntities();

            return await base.SaveChangesAsync(cancellationToken);
        }


        public Task UpdateDatetimeEntities()
        {
            foreach (var entry in ChangeTracker.Entries().Where(entry => entry.Entity.GetType().GetProperty("CreatedAt") != null || entry.Entity.GetType().GetProperty("UpdateAt") != null))
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Property("CreatedAt").IsModified = false;
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }

            return Task.CompletedTask;
        }

    }
}
