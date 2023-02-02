using MaterialPurchase.Domain.BaseEntities;
using MaterialPurchase.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Response
{
    public record CompletePurchaseResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        ProviderResponse Provider,
        ConstructionResponse Construction,
        IEnumerable<ItemMaterialPurchaseResponse> Materials,
        decimal Freight,
        PurchaseStatus Status,
        DateTime? LimitDeliveryDate,
        IEnumerable<AuthUserGroupResponse> AuthorizationUserGroups,
        IEnumerable<PurchaseDeliveryItemResponse> PurchaseDeliveries
    );
}

