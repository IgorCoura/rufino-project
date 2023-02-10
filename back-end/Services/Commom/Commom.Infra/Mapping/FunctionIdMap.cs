using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commom.Infra.Mapping
{
    public class FunctionIdMap : EntityMap<FunctionId> 
    {
        public override void Configure(EntityTypeBuilder<FunctionId> builder)
        {
            base.Configure(builder);

            builder.HasIndex(x => x.Name)
                .IsUnique();

            builder.Property(x => x.Name)
                .HasMaxLength(50)
                .IsRequired();

             
           
        }
    }
}
