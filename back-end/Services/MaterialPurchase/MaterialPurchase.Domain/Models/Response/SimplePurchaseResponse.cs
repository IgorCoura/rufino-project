using MaterialPurchase.Domain.Enum;

namespace MaterialPurchase.Domain.Models.Response
{
    public record SimplePurchaseResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        CompanyResponse Company,
        ProviderResponse Provider,
        ConstructionResponse Construction,
        decimal Freight,
        PurchaseStatus Status,
        DateTime? LimitDeliveryDate
    );
}
