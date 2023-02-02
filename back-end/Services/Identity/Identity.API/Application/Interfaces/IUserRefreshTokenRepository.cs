using Commom.Domain.BaseEntities;
using Identity.API.Application.Entities;

namespace Identity.API.Application.Interfaces
{
    public interface IUserRefreshTokenRepository : IBaseRepository<UserRefreshToken>
    {
    }
}
