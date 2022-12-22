using Commom.Infra.Base;
using Identity.API.Application.Entities;
using Identity.API.Application.Interfaces;

namespace Identity.API.Infrastructure.Repository
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(BaseContext context) : base(context)
        {
        }
    }
}
