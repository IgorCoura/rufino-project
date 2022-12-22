using Commom.Infra.Base;
using Identity.API.Application.Entities;
using Identity.API.Application.Interfaces;
using Identity.API.Infrastructure.Context;

namespace Identity.API.Infrastructure.Repository
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationContext context) : base(context)
        {
        }
    }
}
