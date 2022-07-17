using BuildManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Mapping
{
    public class MaterialMap : EntityMap<Material>
    {
        public new void Configure(EntityTypeBuilder<Material> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Unity)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.WorkHours)
                .IsRequired();
        }
    }
}
