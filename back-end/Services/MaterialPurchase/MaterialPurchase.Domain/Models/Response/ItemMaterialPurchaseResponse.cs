namespace MaterialPurchase.Domain.Models.Response
{
    public record ItemMaterialPurchaseResponse
    (
        Guid Id,
        MaterialResponse? Material,
        decimal UnitPrice,
        BrandResponse? Brand,
        double Quantity
    );
}
