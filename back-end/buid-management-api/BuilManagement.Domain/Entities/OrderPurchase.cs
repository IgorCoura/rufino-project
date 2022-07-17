using BuildManagement.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Entities
{
    public class OrderPurchase : Entity
    {
        public Provider? Provider { get; set; }
        public Guid ProviderId { get; set; }
        public Construction? Construction { get; set; }
        public Guid ConstructionId { get; set; }
        public IEnumerable<MaterialPurchase> Material { get; set; } = new List<MaterialPurchase>();
        public decimal Freight { get; set; }
    }
}
