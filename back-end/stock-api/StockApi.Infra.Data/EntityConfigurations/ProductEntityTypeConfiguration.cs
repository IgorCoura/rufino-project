using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockApi.Domain.Entities;
using StockApi.Infra.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockApi.Infra.Data.EntityConfigurations
{
    public class ProductEntityTypeConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired();
            builder.Property(x => x.Description).IsRequired();
            builder.Property(x => x.Section).IsRequired();
            builder.Property(x => x.Category).IsRequired();
            builder.Property(x => x.Unity).IsRequired();
            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.ModificationDate).IsRequired();
        }
    }
}
