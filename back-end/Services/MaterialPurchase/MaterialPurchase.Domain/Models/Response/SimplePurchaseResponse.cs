using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Domain.Models.Response
{
    public record SimplePurchaseResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        ProviderResponse Provider,
        ConstructionResponse Construction,
        decimal Freight,
        PurchaseStatus Status,
        DateTime? LimitDeliveryDate
    );
}
