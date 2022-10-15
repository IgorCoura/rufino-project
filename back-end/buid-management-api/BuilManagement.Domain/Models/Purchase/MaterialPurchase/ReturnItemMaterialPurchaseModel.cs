using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Purchase.MaterialPurchase
{
    public record ReturnItemMaterialPurchaseModel(
        Guid Id,
        Guid MaterialId,
        decimal UnitPrice,
        Guid BrandId,
        int Quantity,
        int QuantityNotReceived,
        string Status
        );

}
