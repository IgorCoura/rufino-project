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
    public class ConstructionMap : EntityMap<Construction>
    {
        public override void Configure(EntityTypeBuilder<Construction> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.CorporateName)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.NickName)
                .HasMaxLength(50)
                .IsRequired();

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

            builder.OwnsOne(x => x.DeliveryAddress, a =>
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

            builder.HasMany(x => x.CompanyPermissions)
                .WithOne()
                .HasForeignKey(x => x.ConstructionId);

        }
    }
}
