using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Infra.Mapping
{
    public class PurchaseUserAuthorizationMap : EntityMap<PurchaseUserAuthorization>
    {
        public override void Configure(EntityTypeBuilder<PurchaseUserAuthorization> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.AuthorizationStatus)
                .IsRequired();

            builder.Property(x => x.Comment)
                .HasMaxLength(250)
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

            builder.HasOne(x => x.AuthorizationUserGroup)
                .WithMany(x => x.UserAuthorizations)
                .HasForeignKey(x => x.AuthorizationUserGroupId)
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);
        }
    }
}
