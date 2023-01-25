using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Infra.Mapping;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Identity.API.Infrastructure.Context
{
    public class ApplicationContext : BaseContext
    {
        public DbSet<AuthorizationUserGroup> AuthorizationUserGroups { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Construction> Constructions { get; set; }
        public DbSet<ItemMaterialPurchase> ItemMaterialPurchases { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<PurchaseDeliveryItem> PurchaseDeliveryItems { get; set; } 
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<UserAuthorization> UserAuthorizations { get; set; }
        public DbSet<User> Users { get; set; }

        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new AuthorizationUserGroupMap());
            modelBuilder.ApplyConfiguration(new BrandMap());
            modelBuilder.ApplyConfiguration(new ConstructionMap());
            modelBuilder.ApplyConfiguration(new ItemMaterialPurchaseMap());
            modelBuilder.ApplyConfiguration(new MaterialMap());
            modelBuilder.ApplyConfiguration(new ProviderMap());
            modelBuilder.ApplyConfiguration(new PurchaseDeliveryItemMap());
            modelBuilder.ApplyConfiguration(new PurchaseMap());
            modelBuilder.ApplyConfiguration(new UserAuthorizationMap());
            modelBuilder.ApplyConfiguration(new UserMap()); 

            modelBuilder
                .Entity<User>()
                .HasData(new User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    CreatedAt = DateTime.ParseExact("13/10/2021", "dd/MM/yyyy", CultureInfo.InvariantCulture),
                    UpdatedAt = DateTime.ParseExact("13/10/2021", "dd/MM/yyyy", CultureInfo.InvariantCulture),
                    Role = "11"
                });
        }
    }
}
 