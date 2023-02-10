using Commom.Domain.BaseEntities;
using MaterialPurchase.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Entities
{
    public class ConstructionUserAuthorization : Entity
    {
        public ConstructionAuthUserGroup? AuthorizationUserGroup { get; set; }
        public Guid AuthorizationUserGroupId { get; set; }
        public User? User { get; set; }
        public Guid UserId { get; set; }
        public UserAuthorizationStatus AuthorizationStatus { get; set; } = UserAuthorizationStatus.Pending;
        public string Comment { get; set; } = string.Empty;
        public UserAuthorizationPermissions Permissions { get; set; }

    }
}
