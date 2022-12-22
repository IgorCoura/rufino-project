using Commom.Infra.Base;
using Identity.API.Application.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.API.Infrastructure.Mapping
{
    public class UserMap : EntityMap<User>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.Username)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Password)
                .HasMaxLength(86)
                .IsRequired();

        }
    }
}
