using MaterialPurchase.Domain.Enum;

namespace MaterialPurchase.Domain.Models.Response
{
    public record CompletePurchaseResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        CompanyResponse Company,
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

