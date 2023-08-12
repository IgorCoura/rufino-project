using Commom.Infra.Base;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustSuprManager.Infra.Context
{
    public class CustSuprManagerContext : BaseContext
    {

        public CustSuprManagerContext(DbContextOptions<CustSuprManagerContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
