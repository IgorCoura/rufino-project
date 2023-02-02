using Commom.Infra.Base;
using MaterialPurchase.Domain.BaseEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Infra.Mapping
{

    public class PurchaseAuthorizationUserGroupMap : EntityMap<PurchaseAuthUserGroup>
    {
        public override void Configure(EntityTypeBuilder<PurchaseAuthUserGroup> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Priority)
                .IsRequired();

            builder.HasOne(x => x.Purchase)
                .WithMany(x => x.AuthorizationUserGroups)
                .HasForeignKey(x => x.PurchaseId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);
        }
    }
}
