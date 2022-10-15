using BuildManagement.Domain.Entities;
using BuildManagement.Domain.Entities.Purchase;
using BuildManagement.Domain.SeedWork;
using BuildManagement.Infra.Data.Mapping;
using BuildManagement.Infra.Data.Mapping.Purchase;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Context
{
    public class ApplicationContext : DbContext, IUnitOfWork
    {
        public DbSet<Brand>? Brand { get; set; }
        public DbSet<Construction>? Construction { get; set; }
        public DbSet<Job>? Job { get; set; }
        public DbSet<MaterialItem>? MaterialItem { get; set; }
        public DbSet<Material>? Material { get; set; }
        public DbSet<ItemMaterialPurchase>? ItemMaterialPurchase { get; set; }
        public DbSet<MaterialPurchase>? MaterialPurchase { get; set; }
        public DbSet<Provider>? Provider { get; set; }
        public DbSet<Worker>? Worker { get; set; }
        public DbSet<WorkersBond>? WorkersBond { get; set; }
        public DbSet<User>? User { get; set; }


        public ApplicationContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new BrandMap());
            modelBuilder.ApplyConfiguration(new ConstructionMap());
            modelBuilder.ApplyConfiguration(new JobMap());
            modelBuilder.ApplyConfiguration(new MaterialItemMap());
            modelBuilder.ApplyConfiguration(new MaterialMap());
            modelBuilder.ApplyConfiguration(new ItemMaterialPurchaseMap());
            modelBuilder.ApplyConfiguration(new MaterialPurchaseMap());
            modelBuilder.ApplyConfiguration(new ProviderMap());
            modelBuilder.ApplyConfiguration(new WorkerMap());
            modelBuilder.ApplyConfiguration(new WorkersBondMap());
            modelBuilder.ApplyConfiguration(new UserMap());
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries().Where(entry => entry.Entity.GetType().GetProperty("CreateAt") != null || entry.Entity.GetType().GetProperty("UpdateAt") != null))
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property("CreateAt").CurrentValue = DateTime.UtcNow;
                    entry.Property("UpdateAt").CurrentValue = DateTime.UtcNow;
                }

                if (entry.State == EntityState.Modified)
                {
                    entry.Property("CreateAt").IsModified = false;
                    entry.Property("UpdateAt").CurrentValue = DateTime.UtcNow;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }

    }
}
