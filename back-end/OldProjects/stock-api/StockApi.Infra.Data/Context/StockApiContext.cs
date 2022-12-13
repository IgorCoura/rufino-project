using Microsoft.EntityFrameworkCore;
using StockApi.Domain.Entities;
using StockApi.Domain.Seedwork;
using StockApi.Infra.Data.EntityConfigurations;

namespace StockApi.Infra.Data.Context
{
    public class StockApiContext : DbContext, IUnitOfWork
    {
        public DbSet<Worker> Worker => Set<Worker>();
        public DbSet<Product> Product => Set<Product>();
        public DbSet<ProductTransaction> ProductTransaction => Set<ProductTransaction>(); 

        public StockApiContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new ProductEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ProductTransactionEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new WorkerEntityTypeConfiguration());
        }

        public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
        {
            await base.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
