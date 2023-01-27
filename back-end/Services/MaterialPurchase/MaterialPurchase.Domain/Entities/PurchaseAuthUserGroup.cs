using Commom.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Entities
{
    public class PurchaseAuthUserGroup : Entity
    {
        public Purchase Purchase { get; set; }
        public Guid PurchaseId { get; set; }
        public int Priority { get; set; }
        public IEnumerable<PurchaseUserAuthorization> UserAuthorizations { get; set; } = new List<PurchaseUserAuthorization>();
    }
}
 