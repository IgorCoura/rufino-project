using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Infra.Mapping
{
    internal class UsePermissionMap : EntityMap<UsePermission>
    {
        public override void Configure(EntityTypeBuilder<UsePermission> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.UserId);

            builder.HasMany(x => x.FunctionsIds)
                .WithOne()
                .HasForeignKey("UsePermissionId");
        }
    }
}
