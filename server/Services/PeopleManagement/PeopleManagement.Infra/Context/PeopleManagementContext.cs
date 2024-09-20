using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate;
using PeopleManagement.Domain.SeedWord;
using PeopleManagement.Infra.Extension;
using PeopleManagement.Infra.Mapping;
using System.Data;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;

namespace PeopleManagement.Infra.Context
{
    public class PeopleManagementContext : DbContext, IUnitOfWork
    {
        public const string DEFAULT_SCHEMA = "people_management";

        public DbSet<ArchiveCategory> ArchiveCategories { get; set; }
        public DbSet<Archive> Archives { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<DocumentUnit> DocumentsUnits { get; set; }
        public DbSet<DocumentTemplate> DocumentTemplates { get; set; }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<RequireDocuments> RequireDocuments { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Workplace> Workplaces { get; set; }
        
        
       

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
            modelBuilder.ApplyConfiguration(new ArchiveCategoryMap());
            modelBuilder.ApplyConfiguration(new ArchiveMap());
            modelBuilder.ApplyConfiguration(new CompanyMap());
            modelBuilder.ApplyConfiguration(new DepartmentMap());
            modelBuilder.ApplyConfiguration(new EmployeeMap());            
            modelBuilder.ApplyConfiguration(new PositionMap());
            modelBuilder.ApplyConfiguration(new RoleMap());
            modelBuilder.ApplyConfiguration(new WorkplaceMap());
            modelBuilder.ApplyConfiguration(new DocumentMap());
            modelBuilder.ApplyConfiguration(new DocumentUnitMap());
            modelBuilder.ApplyConfiguration(new RequireDocumentsMap());
            modelBuilder.ApplyConfiguration(new DocumentTemplateMap());
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

        public async Task<int> SaveChangesWithoutDispatchEventsAsync(CancellationToken cancellationToken = default)
        {

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
