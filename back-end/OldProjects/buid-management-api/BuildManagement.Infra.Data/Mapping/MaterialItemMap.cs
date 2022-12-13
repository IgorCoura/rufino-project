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
        public override void Configure(EntityTypeBuilder<MaterialItem> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.Pricing)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(x => x.WorkHours)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.HasOne(x => x.Material)
                .WithMany()
                .HasForeignKey(x => x.MaterialId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

        }
    }
}
