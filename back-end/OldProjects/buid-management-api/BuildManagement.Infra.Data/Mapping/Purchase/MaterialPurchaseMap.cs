using BuildManagement.Domain.Entities.Purchase;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Mapping.Purchase
{
    public class MaterialPurchaseMap : EntityMap<MaterialPurchase>
    {
        public override void Configure(EntityTypeBuilder<MaterialPurchase> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Freight)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.HasOne(x => x.Provider)
                .WithMany()
                .HasForeignKey(x => x.ProviderId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

            builder.HasOne(x => x.Construction)
                .WithMany()
                .HasForeignKey(x => x.ConstructionId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);



        }
    }
}
