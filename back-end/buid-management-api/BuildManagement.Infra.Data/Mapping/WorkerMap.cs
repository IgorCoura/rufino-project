using BuildManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Mapping
{
    public class WorkerMap : EntityMap<Worker>
    {
        public new void Configure(EntityTypeBuilder<Worker> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.HourlyWage)
                .IsRequired();

            builder.Property(x => x.Office)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Cpf)
                .HasMaxLength(14)
                .IsRequired();

        }
    }
}
