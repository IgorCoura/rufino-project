using BuildManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Mapping
{
    public class MaterialPurchaseMap : EntityMap<MaterialPurchase>
    {
        public new void Configure(EntityTypeBuilder<MaterialPurchase> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.UnitPricing)
                .IsRequired();

            builder.Property(x => x.Quantity)
                .IsRequired();


        }
    }
}
