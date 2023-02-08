using Commom.Infra.Base;
using MaterialControl.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControl.Infra.Mapping
{
    public class UnityMap : EntityMap<Unity>
    {
        public override void Configure(EntityTypeBuilder<Unity> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Name)
                .HasMaxLength(25)
                .IsRequired();
        }

    }
}
