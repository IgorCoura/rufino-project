using BuildManagement.Domain.Models.Brand;
using BuildManagement.Domain.Models.Material;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Purchase.ItemMaterialPurchase
{
    public record MaterialItemPurchaseModel(
        Guid MaterialId,
        Guid BrandId,
        decimal UnitPrice,
        int Quantity
    );
  
}
