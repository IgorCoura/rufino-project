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
using PeopleManagement.Infra.Mapping;
using System.Data;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate;
using PeopleManagement.Infra.Idempotency;
using PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate;
using PeopleManagement.Domain.AggregatesModel.WebHookAggregate;
using PeopleManagement.Infra.ExtensionClass;

namespace PeopleManagement.Infra.Context
{
    public class PeopleManagementContext : DbContext, IUnitOfWork
    {
        public const string DEFAULT_SCHEMA = "people_management";

        public DbSet<ArchiveCategory> ArchiveCategories { get; set; } = null!;
        public DbSet<Archive> Archives { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<DocumentGroup> DocumentGroups { get; set; } = null!;
        public DbSet<DocumentUnit> DocumentsUnits { get; set; } = null!;
        public DbSet<DocumentTemplate> DocumentTemplates { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Position> Positions { get; set; } = null!;
        public DbSet<RequireDocuments> RequireDocuments { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<Workplace> Workplaces { get; set; } = null!;
        public DbSet<ClientRequest> ClientRequests { get; set; } = null!;
        public DbSet<WebHook>   WebHooks { get; set; } = null!;

        private readonly IMediator? _mediator;
        private readonly IDbContextTransaction? _currentTransaction;

        public IDbContextTransaction? GetCurrentTransaction() => _currentTransaction;
        public bool HasActiveTransaction => _currentTransaction != null;

        public PeopleManagementContext(DbContextOptions<PeopleManagementContext> options, IMediator? mediator) : base(options)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new ArchiveCategoryMap());
            modelBuilder.ApplyConfiguration(new ArchiveMap());
            modelBuilder.ApplyConfiguration(new ClientRequestMap());
            modelBuilder.ApplyConfiguration(new CompanyMap());
            modelBuilder.ApplyConfiguration(new DepartmentMap());
            modelBuilder.ApplyConfiguration(new DocumentGroupMap());
            modelBuilder.ApplyConfiguration(new EmployeeMap());            
            modelBuilder.ApplyConfiguration(new PositionMap());
            modelBuilder.ApplyConfiguration(new RoleMap());
            modelBuilder.ApplyConfiguration(new WorkplaceMap());
            modelBuilder.ApplyConfiguration(new DocumentMap());
            modelBuilder.ApplyConfiguration(new DocumentUnitMap());
            modelBuilder.ApplyConfiguration(new RequireDocumentsMap());
            modelBuilder.ApplyConfiguration(new DocumentTemplateMap());
            modelBuilder.ApplyConfiguration(new WebHookMap());

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
