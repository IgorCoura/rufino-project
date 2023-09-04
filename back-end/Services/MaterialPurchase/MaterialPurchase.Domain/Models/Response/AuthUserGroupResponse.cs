namespace MaterialPurchase.Domain.Models.Response
{
    public record AuthUserGroupResponse
    (
        Guid Id,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int Priority,
        IEnumerable < UserAuthorizationResponse > UserAuthorizations
    );
}
