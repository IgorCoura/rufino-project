using Commom.Domain.BaseEntities;
using Commom.Infra.Base;
using MaterialControl.Domain.Entities;
using MaterialControl.Infra.Mapping;
using MaterialPurchase.Domain.BaseEntities;
using Microsoft.EntityFrameworkCore;

namespace MaterialControl.Infra.Context
{
    public class MaterialControlContext : BaseContext
    {
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Unity> Unities { get; set; }
        public DbSet<User> Users { get; set; }

        
        public MaterialControlContext(DbContextOptions<MaterialControlContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new BrandMap());
            modelBuilder.ApplyConfiguration(new MaterialMap());
            modelBuilder.ApplyConfiguration(new UnityMap());
            modelBuilder.ApplyConfiguration(new UserMap());

            //DATA FOR TESTS 
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (env != null && env.Equals("Development"))
            {
                var ids = new string[]
                {
                "2999"
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
