using BuildManagement.Domain.Entities.Purchase;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Mapping.Purchase
{
    public class ItemMaterialPurchaseMap : EntityMap<ItemMaterialPurchase>
    {
        public override void Configure(EntityTypeBuilder<ItemMaterialPurchase> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.UnitPrice)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.HasOne(x => x.Material)
                .WithMany()
                .HasForeignKey(x => x.MaterialId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
            
            builder.HasOne(x => x.Brand)
                .WithMany()
                .HasForeignKey(x => x.BrandId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

            builder.HasOne(x => x.MaterialPurchase)
                .WithMany(x => x.Material)
                .HasForeignKey(x => x.MaterialPurchaseId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

        }
    }
}
