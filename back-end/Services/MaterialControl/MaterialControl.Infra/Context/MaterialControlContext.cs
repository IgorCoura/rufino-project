using Commom.Domain.BaseEntities;
using Commom.Infra.Base;
using MaterialPurchase.Domain.BaseEntities;
using Microsoft.EntityFrameworkCore;

namespace MaterialControl.Infra.Context
{
    public class MaterialControlContext : BaseContext
    {
        public MaterialControlContext(DbContextOptions<MaterialControlContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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
