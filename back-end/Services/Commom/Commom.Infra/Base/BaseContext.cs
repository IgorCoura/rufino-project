using Commom.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commom.Infra.Base
{
    public class BaseContext : DbContext, IUnitOfWork
    {
        public BaseContext(DbContextOptions options) : base(options)
        {

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
