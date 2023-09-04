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
    public class UsePermissionMap : EntityMap<UsePermission>
    {
        public override void Configure(EntityTypeBuilder<UsePermission> builder)
        {
            base.Configure(builder);

            builder.HasMany(x => x.FunctionsIds)
                .WithOne()
                .HasForeignKey("UsePermissionId");

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);
        }
    }
}
