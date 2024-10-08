﻿using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaterialPurchase.Infra.Mapping
{
    public class BrandMap : EntityMap<Brand>
    {
        public override void Configure(EntityTypeBuilder<Brand> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Name)
                .HasMaxLength(50)
                .IsRequired();

        }
    }
}
