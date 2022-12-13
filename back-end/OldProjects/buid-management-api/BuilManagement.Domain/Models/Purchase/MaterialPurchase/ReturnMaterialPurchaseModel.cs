using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Purchase.MaterialPurchase
{
    public record ReturnMaterialPurchaseModel(
        Guid Id,
        Guid ProviderId,
        Guid ConstructionId,
        List<ReturnItemMaterialPurchaseModel> Materials,
        decimal Freight,
        string Status
        );
}
