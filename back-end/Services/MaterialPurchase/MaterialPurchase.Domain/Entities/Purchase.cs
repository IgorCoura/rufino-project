using Commom.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Entities
{
    public class Purchase : Entity
    {
        public Provider? Provider { get; set; }
        public Guid ProviderId { get; set; }
        public Construction? Construction { get; set; }
        public Guid ConstructionId { get; set; }
        public virtual List<MaterialPurchase> Materials { get; set; } = new List<MaterialPurchase>();
        public decimal Freight { get; set; }
    }
}
