using Commom.Infra.Base;
using Identity.API.Application.Entities;
using Identity.API.Application.Interfaces;

namespace Identity.API.Infrastructure.Repository
{
    public class UserRefreshTokenRepository : BaseRepository<UserRefreshToken>, IUserRefreshTokenRepository
    {
        public UserRefreshTokenRepository(BaseContext context) : base(context)
        {
        }
    }
}
