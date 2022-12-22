using Commom.Infra.Base;
using Identity.API.Application.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.API.Infrastructure.Mapping
{
    public class UserRefreshTokenMap : EntityMap<UserRefreshToken>
    {
        public override void Configure(EntityTypeBuilder<UserRefreshToken> builder)
        {
            base.Configure(builder);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.ExpireDate)
                .IsRequired();

        }
    }
}
