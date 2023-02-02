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

    public class ConstructionAuthorizationUserGroupMap : EntityMap<ConstructionAuthUserGroup>
    {
        public override void Configure(EntityTypeBuilder<ConstructionAuthUserGroup> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Priority)
                .IsRequired();

            builder.HasOne(x => x.Construction)
                .WithMany(x => x.PurchasingAuthorizationUserGroups)
                .HasForeignKey(x => x.ConstructionId)
                .IsRequired()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);
        }
    }
}
