using BuildManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Mapping
{
    public class OrderPurchaseMap : EntityMap<OrderPurchase>
    {
        public new void Configure(EntityTypeBuilder<OrderPurchase> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Freight)
                .IsRequired();

        }
    }
}
