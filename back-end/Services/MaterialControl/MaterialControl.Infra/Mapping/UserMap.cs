using Commom.Infra.Base;
using MaterialControl.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaterialControl.Infra.Mapping
{
    public class UserMap : EntityMap<User>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Username)
                .HasMaxLength(50)
                .IsRequired();

        }
    }
}
