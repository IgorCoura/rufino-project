namespace Identity.API.Application.Model
{
    public record TokenModel
    (
        string AccessToken,
        string RefreshToken
    );
}
