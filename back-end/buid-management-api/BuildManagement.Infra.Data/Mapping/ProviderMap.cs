using BuildManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Mapping
{
    public class ProviderMap : EntityMap<Provider>
    {
        public new void Configure(EntityTypeBuilder<Provider> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(x => x.Cpf)
                .HasMaxLength(14)
                .IsRequired();

            builder.OwnsOne(x => x.Address, x =>
            {
                x.Property<Guid>("ProviderId");
                x.WithOwner();
            });
        }
    }
}
