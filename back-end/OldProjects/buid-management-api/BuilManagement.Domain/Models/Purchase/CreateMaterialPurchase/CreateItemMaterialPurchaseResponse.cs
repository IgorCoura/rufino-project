using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Purchase.CreateMaterialPurchase
{
    public record CreateItemMaterialPurchaseResponse(
            Guid Id,
            Guid MaterialId,
            Guid BrandId,
            decimal UnitPrice,
            int Quantity
        );
}
