using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Domain.Models.Purchase.CreateMaterialPurchase
{
    public record CreateMaterialPurchaseRequest
    (
        Guid ProviderId,
        Guid ConstructionId,
        List<CreateItemMaterialPurchaseRequest> Materials,
        decimal Freight
    );

}
