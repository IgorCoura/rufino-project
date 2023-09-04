using MaterialPurchase.Domain.Enum;

namespace MaterialPurchase.Domain.Models.Response
{
    public record UserAuthorizationResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        UserResponse? User,
        UserAuthorizationStatus AuthorizationStatus,
        string Comment
    );
}
