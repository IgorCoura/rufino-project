using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaterialPurchase.Infra.Mapping
{
    public class PurchaseDeliveryItemMap : EntityMap<PurchaseDeliveryItem>
    {
        public override void Configure(EntityTypeBuilder<PurchaseDeliveryItem> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Quantity)
                .HasPrecision(13, 2)
                .IsRequired();

            builder.HasOne(x => x.MaterialPurchase)
                .WithMany()
                .HasForeignKey(x => x.MaterialPurchaseId)
                .IsRequired();

            builder.HasOne(x => x.Purchase)
                .WithMany(x => x.PurchaseDeliveries)
                .HasForeignKey(x => x.PurchaseId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);

            builder.HasOne(x => x.Receiver)
                .WithMany()
                .HasForeignKey(x => x.ReceiverId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

        }
    }
}
