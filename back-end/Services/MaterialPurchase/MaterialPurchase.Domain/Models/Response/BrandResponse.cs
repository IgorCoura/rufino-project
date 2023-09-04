namespace MaterialPurchase.Domain.Models.Response
{
    public record BrandResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string Name
    );
}
