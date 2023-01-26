using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaterialPurchase.Infra.Mapping
{
    public class PurchaseMap : EntityMap<Purchase>
    {
        public override void Configure(EntityTypeBuilder<Purchase> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Freight)
                .HasPrecision(18, 6)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.LimitDeliveryDate)
                .IsRequired(false);

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
