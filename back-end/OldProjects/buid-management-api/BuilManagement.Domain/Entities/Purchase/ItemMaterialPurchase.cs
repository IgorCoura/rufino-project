using BuildManagement.Domain.Entities.Enum;
using BuildManagement.Domain.SeedWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Entities.Purchase
{
    public class ItemMaterialPurchase : Entity
    {
        public Material? Material { get; set; }
        public Guid MaterialId { get; set; }
        public MaterialPurchase? MaterialPurchase { get; set; }
        public Guid MaterialPurchaseId { get; set; }
        public decimal UnitPrice { get; set; }
        public Brand? Brand { get; set; }
        public Guid BrandId { get; set; }
        public int Quantity { get; set; }
        public int QuantityNotReceived { get; set; } = 0;
        public MaterialStatus Status { get; set; } = MaterialStatus.Open;
    }
}
