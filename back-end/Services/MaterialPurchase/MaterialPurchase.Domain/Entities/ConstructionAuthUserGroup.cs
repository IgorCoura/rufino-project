using Commom.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Entities
{
    public class ConstructionAuthUserGroup : Entity
    {
        public Construction? Construction { get; set; }
        public Guid ConstructionId { get; set; }
        public int Priority { get; set; }
        public IEnumerable<ConstructionUserAuthorization> UserAuthorizations { get; set; } = new List<ConstructionUserAuthorization>();
    }
}
