using BuildManagement.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Entities
{
    public class MaterialPurchase: Entity
    {
        public Material? Material { get; set; }
        public Guid MaterialId { get; set; }
        public Brand? Brand { get; set; }
        public Guid BrandId { get; set; }
        public OrderPurchase? OrderPurchase { get; set; }
        public Guid OrderPurchaseId { get; set; }
        public decimal UnitPricing { get; set; }
        public int Quantity { get; set; }
    }
}
