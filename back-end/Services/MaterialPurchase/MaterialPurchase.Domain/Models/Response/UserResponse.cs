namespace MaterialPurchase.Domain.Models.Response
{
    public record UserResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string Username,
        string Role
    );
}
