using Commom.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Entities
{
    public class MaterialPurchase : Entity
    {
        public Material? Material { get; set; }
        public Guid MaterialId { get; set; }
        public Purchase? Purchase { get; set; }
        public Guid PurchaseId { get; set; }
        public decimal UnitPrice { get; set; }
        public Brand? Brand { get; set; }
        public Guid BrandId { get; set; }
        public int Quantity { get; set; }
        public int QuantityReceived { get; set; } = 0;
    }
}
