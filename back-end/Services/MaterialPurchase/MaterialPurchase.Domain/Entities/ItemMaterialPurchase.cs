using Commom.Domain.BaseEntities;
using MaterialPurchase.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.BaseEntities
{
    public class ItemMaterialPurchase : Entity
    {
        public Material? Material { get; set; }
        public Guid MaterialId { get; set; }
        public Purchase? Purchase { get; set; }
        public Guid PurchaseId { get; set; }    
        public decimal UnitPrice { get; set; }
        public Brand? Brand { get; set; }
        public Guid BrandId { get; set; }
        public double Quantity { get; set; }
    }
}
