using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Response
{
    public record PurchaseWithMaterialResponse
    (
        Guid Id,
        ProviderResponse Provider,
        ConstructionResponse Construction,
        IEnumerable<ItemMaterialPurchaseResponse> Materials,
        decimal Freight,
        PurchaseStatus Status,
        DateTime? LimitDeliveryDate
    );
}

