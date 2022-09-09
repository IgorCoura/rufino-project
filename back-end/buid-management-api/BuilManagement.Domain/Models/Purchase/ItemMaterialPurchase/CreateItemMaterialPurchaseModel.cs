using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Purchase.ItemMaterialPurchase
{
    public record CreateItemMaterialPurchaseModel(
        Guid MaterialId, 
        Guid BrandId,
        decimal UnitPrice,
        int Quantity
   );
}
