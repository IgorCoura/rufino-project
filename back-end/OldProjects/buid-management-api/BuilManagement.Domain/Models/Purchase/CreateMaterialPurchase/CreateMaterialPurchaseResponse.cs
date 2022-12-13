using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Purchase.CreateMaterialPurchase
{
    public record CreateMaterialPurchaseResponse
    (
        Guid Id,
        Guid ProviderId,
        Guid ConstructionId,
        List<CreateItemMaterialPurchaseResponse> Materials,
        decimal Freight
    );

}
