using BuildManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Mapping
{
    internal class WorkersBondMap : EntityMap<WorkersBond>
    {
        public new void Configure(EntityTypeBuilder<WorkersBond> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.InitBond)
                .IsRequired();

            builder.Property(x => x.EndBond)
                .IsRequired();

        }
    }
}
