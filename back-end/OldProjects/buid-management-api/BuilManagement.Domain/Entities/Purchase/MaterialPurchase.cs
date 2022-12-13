using BuildManagement.Domain.Entities.Enum;
using BuildManagement.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Entities.Purchase
{
    public class MaterialPurchase : Entity
    {
        public Provider? Provider { get; set; }
        public Guid ProviderId { get; set; }
        public Construction? Construction { get; set; }
        public Guid ConstructionId { get; set; }
        public virtual List<ItemMaterialPurchase> Materials { get; set; } = new List<ItemMaterialPurchase>();
        public decimal Freight { get; set; }
        public MaterialPurchaseStatus Status { get; set; }
    }
}
