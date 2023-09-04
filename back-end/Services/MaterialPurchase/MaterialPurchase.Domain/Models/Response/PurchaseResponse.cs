namespace MaterialPurchase.Domain.Models.Response
{
    public record PurchaseResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );
}
