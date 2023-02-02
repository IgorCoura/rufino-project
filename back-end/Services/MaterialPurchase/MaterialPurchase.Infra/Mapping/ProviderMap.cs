using Commom.Infra.Base;
using MaterialPurchase.Domain.BaseEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaterialPurchase.Infra.Mapping
{
    public class ProviderMap : EntityMap<Provider>
    {
        public override void Configure(EntityTypeBuilder<Provider> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(x => x.Cnpj)
                .HasMaxLength(18)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Site)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(x => x.Phone)
                .HasMaxLength(20);


            builder.OwnsOne(x => x.Address, a =>
            {
                a.Property(v => v.Street)
                .HasMaxLength(50)
                .IsRequired();

                a.Property(v => v.City)
                .HasMaxLength(50)
                .IsRequired();

                a.Property(v => v.State)
                .HasMaxLength(50)
                .IsRequired();

                a.Property(v => v.Country)
                .HasMaxLength(50)
                .IsRequired();

                a.Property(v => v.ZipCode)
                .HasMaxLength(16)
                .IsRequired();
            });

        }
    }
}
