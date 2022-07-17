using BuildManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Mapping
{
    public class ConstructionMap : EntityMap<Construction>
    {
        public new void Configure(EntityTypeBuilder<Construction> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.CorporateName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.NickName)
                .HasMaxLength(200)
                .IsRequired();

            builder.OwnsOne(x => x.Address, x =>
            {
                x.Property<Guid>("ConstructionId");
                x.WithOwner();
            });

        }
    }
}
