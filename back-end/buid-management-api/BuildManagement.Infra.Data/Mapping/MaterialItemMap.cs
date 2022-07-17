using BuildManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Mapping
{
    public class MaterialItemMap : EntityMap<MaterialItem>
    {
        public new void Configure(EntityTypeBuilder<MaterialItem> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.Pricing)
                .IsRequired();

            builder.Property(x => x.WorkHours)
                .IsRequired();



        }
    }
}
