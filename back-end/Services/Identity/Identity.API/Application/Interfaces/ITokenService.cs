using Identity.API.Application.Entities;
using Identity.API.Application.Model;
using System.Security.Claims;

namespace Identity.API.Application.Interfaces
{
    public interface ITokenService
    {
        Task<TokenModel> GenerateTokens(User user, UserRefreshToken userRefreshToken);
        ClaimsPrincipal ValidateRefreshToken(string token);
    }
}
