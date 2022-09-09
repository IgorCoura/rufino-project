using BuildManagement.Domain.Models.Purchase.ItemMaterialPurchase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Purchase.MaterialPurchase
{
    public record CreateMaterialPurchaseModel
    (
        Guid ProviderId,
        Guid ConstructionId,
        List<CreateItemMaterialPurchaseModel> MaterialPurchase,
        decimal Freight
    );
}

