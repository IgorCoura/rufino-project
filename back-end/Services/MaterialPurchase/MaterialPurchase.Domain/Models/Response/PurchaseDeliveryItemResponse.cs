namespace MaterialPurchase.Domain.Models.Response
{
    public record PurchaseDeliveryItemResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        Guid MaterialPurchaseId,
        double Quantity,
        UserResponse? Receiver
    );
}
