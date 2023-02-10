using MaterialPurchase.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Commom.Domain.BaseEntities;
using Commom.Infra.Base;

namespace Commom.Infra.Mapping
{
    public class RoleMap : EntityMap<Role>
    {
        public override void Configure(EntityTypeBuilder<Role> builder)
        {
            base.Configure(builder);

            builder.HasIndex(x => x.Name)
                .IsUnique();

            builder.Property(x => x.Name)
                .HasMaxLength(50)
                .IsRequired();

            builder.HasMany(x => x.FunctionsIds)
                .WithMany(x => x.Roles);

        }
    }
}
