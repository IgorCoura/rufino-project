using Commom.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Entities
{
    public class AuthorizationUserGroup : Entity
    {
        public IEnumerable<UserAuthorization> UserAuthorizations { get; set; } = new List<UserAuthorization>();
    }
}
