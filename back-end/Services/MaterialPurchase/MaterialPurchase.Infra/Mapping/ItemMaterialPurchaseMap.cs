using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Infra.Mapping
{
    public class ItemMaterialPurchaseMap : EntityMap<ItemMaterialPurchase>
    {
        public override void Configure(EntityTypeBuilder<ItemMaterialPurchase> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Quantity)
                .IsRequired();

            builder.Property(x => x.UnitPrice)
                .HasPrecision(13, 4)
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

            builder.HasOne(x => x.Purchase)
                .WithMany(x => x.Materials)
                .HasForeignKey(x => x.PurchaseId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

        }
    }
}
