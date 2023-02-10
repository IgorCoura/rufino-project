using Commom.Domain.BaseEntities;
using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Infra.Mapping;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace MaterialPurchase.Infra.Context
{
    public class MaterialPurchaseContext : BaseContext
    {
        public DbSet<ConstructionAuthUserGroup> ConstructionAuthUserGroups { get; set; }
        public DbSet<PurchaseAuthUserGroup> PurchaseAuthUserGroups { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Construction> Constructions { get; set; }
        public DbSet<ItemMaterialPurchase> ItemMaterialPurchases { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Provider> Providers { get; set; }
        public DbSet<PurchaseDeliveryItem> PurchaseDeliveryItems { get; set; } 
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseUserAuthorization> PurchaseUserAuthorizations { get; set; }
        public DbSet<ConstructionUserAuthorization> ConstructionUserAuthorizations { get; set; }
        public DbSet<User> Users { get; set; }

        public MaterialPurchaseContext(DbContextOptions<MaterialPurchaseContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new ConstructionAuthorizationUserGroupMap());
            modelBuilder.ApplyConfiguration(new PurchaseAuthorizationUserGroupMap());
            modelBuilder.ApplyConfiguration(new BrandMap());
            modelBuilder.ApplyConfiguration(new ConstructionMap());
            modelBuilder.ApplyConfiguration(new ItemMaterialPurchaseMap());
            modelBuilder.ApplyConfiguration(new MaterialMap());
            modelBuilder.ApplyConfiguration(new ProviderMap());
            modelBuilder.ApplyConfiguration(new PurchaseDeliveryItemMap());
            modelBuilder.ApplyConfiguration(new PurchaseMap());
            modelBuilder.ApplyConfiguration(new PurchaseUserAuthorizationMap());
            modelBuilder.ApplyConfiguration(new ConstructionUserAuthorizationMap());
            modelBuilder.ApplyConfiguration(new UserMap());


            //DATA FOR TESTS 
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (env != null && env.Equals("Development"))
            {
                modelBuilder
                .Entity<User>()
                .HasData(new User
                {
                    Id = Guid.Parse("4922766E-D3BA-4D4C-99B0-093D5977D41F"),
                    Username = "admin",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Role = "admin"
                });

                modelBuilder.Entity<Material>()
                    .HasData(new Material
                    {
                        Id = Guid.NewGuid(),
                        Name = "Tubo de PVC",          
                        Unity = "Metro",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });

                modelBuilder.Entity<Brand>()
                    .HasData(new Brand()
                    {
                        Id = Guid.NewGuid(),
                        Name = "Tigre",
                            CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });

                modelBuilder.Entity<Provider>()
                    .HasData(new Provider()
                    {
                        Id = Guid.Parse("8299C0DC-927D-45DE-B2C8-71C38FAF9384"),
                        Name = "Ponto do Encanador",
                        Description = "description",
                        Cnpj = "02.624.999/0001-23",
                        Email = "ponto@email.com",
                        Site = "Site.com",
                        Phone = "Phone",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });

                modelBuilder.Entity<Provider>().OwnsOne(x => x.Address)
                    .HasData(new
                    {
                        ProviderId = Guid.Parse("8299C0DC-927D-45DE-B2C8-71C38FAF9384"),
                        Street = "Dom Pedro",
                        City = "Piracicaba",
                        State = "Sao Paulo",
                        Country = "Brasil",
                        ZipCode = "99999-000"
                    });

                modelBuilder.Entity<Construction>()
                    .HasData(new Construction()
                    {
                        Id = Guid.Parse("CAD4DA64-E4AB-4B4A-8E83-63FC05FEFA64"),
                        NickName = "Build Ltda",
                        CorporateName = "Build LTDA",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });

                modelBuilder.Entity<Construction>()
                    .OwnsOne(x => x.Address)
                    .HasData(new
                    {
                        ConstructionId = Guid.Parse("CAD4DA64-E4AB-4B4A-8E83-63FC05FEFA64"),
                        Street = "Dom Pedro",
                        City = "Piracicaba",
                        State = "Sao Paulo",
                        Country = "Brasil",
                        ZipCode = "99999-000"
                    });

                modelBuilder.Entity<ConstructionAuthUserGroup>()
                    .HasData(new ConstructionAuthUserGroup()
                    {
                        Id = Guid.Parse("E6389915-3947-46D1-A636-DA6F9AD505AA"),
                        ConstructionId = Guid.Parse("CAD4DA64-E4AB-4B4A-8E83-63FC05FEFA64"),
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });

                modelBuilder.Entity<ConstructionUserAuthorization>()
                    .HasData(new ConstructionUserAuthorization()
                    {
                        Id = Guid.NewGuid(),
                        AuthorizationUserGroupId = Guid.Parse("E6389915-3947-46D1-A636-DA6F9AD505AA"),
                        UserId = Guid.Parse("4922766E-D3BA-4D4C-99B0-093D5977D41F"),
                        AuthorizationStatus = Domain.Enum.UserAuthorizationStatus.Pending,
                        Permissions = Domain.Enum.UserAuthorizationPermissions.Admin,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                var ids = new string[]
                {
                "1001", "1002", "1003", "1004", "1005", "1006",
                "1007", "1008", "1009", "1010", "1011", "1012",
                "1013", "1014", "1015", "1016", "1017", "1018",
                };

                var functionsIdsAdmin = ids.Select(x => new FunctionId()
                {
                    Id = Guid.NewGuid(),
                    Name = x
                }).ToList();

                var rolesAdmin = new Role()
                {
                    Id = Guid.NewGuid(),
                    Name = "admin"
                };

                modelBuilder.Entity<FunctionId>().HasData(
                        functionsIdsAdmin
                    );

                modelBuilder.Entity<Role>().HasData(
                        rolesAdmin
                    );

                var mData = functionsIdsAdmin.Select(x => new
                {
                    RolesId = rolesAdmin.Id,
                    FunctionsIdsId = x.Id
                }).ToList();

                modelBuilder.Entity<Role>()
                    .HasMany(p => p.FunctionsIds)
                    .WithMany(p => p.Roles)
                    .UsingEntity(j => j.HasData(mData));

            }


        }
    }
}
 