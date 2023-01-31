namespace MaterialPurchase.Domain.Models.Request
{
    public record MaterialDraftPurchaseRequest(
        Guid Id,
        Guid MaterialId,
        Guid BrandId,
        decimal UnitPrice,
        double Quantity
        );

}
