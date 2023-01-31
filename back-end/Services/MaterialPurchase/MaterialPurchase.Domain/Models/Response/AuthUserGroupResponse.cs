using MaterialPurchase.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Response
{
    public record AuthUserGroupResponse 
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Priority,
        IEnumerable < UserAuthorizationResponse > UserAuthorizations
    );
}
