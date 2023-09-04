namespace MaterialPurchase.Domain.Models.Response
{
    public record MaterialResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string Name,
        string Unity
    );
}
