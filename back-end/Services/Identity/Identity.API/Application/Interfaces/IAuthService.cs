using Identity.API.Application.Entities;
using Identity.API.Application.Model;
using Identity.API.Application.Service;

namespace Identity.API.Application.Interfaces
{
    public interface IAuthService
    {
        Task<TokenModel> SingIn(LoginModel loginModel);
        Task<TokenModel> RefreshToken(string refreshToken);
        Task SingOutLocal(string refreshToken);
        Task SingOutAll(string refreshToken);
    }
}