
using System.Security.Claims;
using Authenticate.Api.Utils;
using Authenticate.Api.Model;
using Authenticate.Api.Options;
using Microsoft.Extensions.Options;
using Authenticate.Api.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Authenticate.Api.Services
{
    public class AuthService: IAuthService
    {
        private readonly TokenGeneratorOptions _options;

        public AuthService(IOptions<TokenGeneratorOptions> options) 
        {
            _options = options.Value;
        }

        

        public Task<string> Login(LoginModel model)
        {
            
            return Task.FromResult("adsadasd");

        }

        


    }
}
