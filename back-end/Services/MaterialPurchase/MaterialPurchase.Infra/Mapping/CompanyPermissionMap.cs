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
    public class CompanyPermissionMap : EntityMap<CompanyPermission>
    {
        public override void Configure(EntityTypeBuilder<CompanyPermission> builder)
        {
            base.Configure(builder);


            builder.HasMany(x => x.UsePermissions)
                .WithOne()
                .HasForeignKey(x => x.CompanyPermissionId)
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

            builder.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
                
        }
    }
}
