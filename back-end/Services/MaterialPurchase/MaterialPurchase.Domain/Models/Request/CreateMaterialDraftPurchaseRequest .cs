using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Request
{
    public record CreateMaterialDraftPurchaseRequest
    {
        public CreateMaterialDraftPurchaseRequest(Guid materialId, Guid brandId, decimal unitPrice, double quantity)
        {
            MaterialId = materialId;
            BrandId = brandId;
            UnitPrice = unitPrice;
            Quantity = quantity;
        }

        public Guid MaterialId { get; set; }
        public Guid BrandId { get; set; }
        public decimal UnitPrice { get; set; }
        public double Quantity { get; set; }

    }

}
