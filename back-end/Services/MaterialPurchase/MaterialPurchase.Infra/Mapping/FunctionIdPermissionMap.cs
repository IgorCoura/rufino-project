
using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaterialPurchase.Infra.Mapping
{
    public class FunctionIdPermissionMap : EntityMap<FunctionIdPermission>
    {
        public override void Configure(EntityTypeBuilder<FunctionIdPermission> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Name)
                .HasMaxLength(50)
                .IsRequired();
        }
    }
}
