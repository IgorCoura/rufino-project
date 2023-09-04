using Commom.Auth.Entities;
using Commom.Auth.Mapping;
using Commom.Infra.Base;
using Microsoft.EntityFrameworkCore;

namespace CustSuprManager.Infra.Context
{
    public class CustSuprManagerContext : BaseContext
    {
        public DbSet<FunctionId> FunctionIds { get; set; }
        public DbSet<Role> Roles { get; set; }

        public CustSuprManagerContext(DbContextOptions<CustSuprManagerContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new FunctionIdMap());
            modelBuilder.ApplyConfiguration(new RoleMap());
        }
    }
}
