namespace Identity.API.Model
{
    public record TokenModel
    (
        string AccessToken,
        string RefreshToken
    );
}
