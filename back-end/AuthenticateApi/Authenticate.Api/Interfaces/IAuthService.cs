using Authenticate.Api.Model;

namespace Authenticate.Api.Interfaces
{
    public interface IAuthService
    {
        Task<string> Login(LoginModel model);
    }
}
