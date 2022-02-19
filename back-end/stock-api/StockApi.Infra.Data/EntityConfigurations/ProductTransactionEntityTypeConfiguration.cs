using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockApi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Infra.Data.EntityConfigurations
{
    public class ProductTransactionEntityTypeConfiguration : IEntityTypeConfiguration<ProductTransaction>
    {
        public void Configure(EntityTypeBuilder<ProductTransaction> builder)
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.QuantityVariation).IsRequired();
            builder.Property(t => t.date).IsRequired();

            builder.HasOne(x => x.Product).WithMany();
            builder.HasOne(x => x.Responsible).WithMany();
            builder.HasOne(x => x.Taker).WithMany();
        }
    }
}
