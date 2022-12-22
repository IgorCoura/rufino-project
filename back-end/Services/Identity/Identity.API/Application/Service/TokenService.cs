using Commom.API.Authentication;
using Identity.API.Application.Entities;
using Identity.API.Application.Interfaces;
using Identity.API.Application.Model;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NetDevPack.Security.Jwt.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.API.Application.Service
{
    public class TokenService : ITokenService
    {
        private readonly AuthenticationOptions _authenticationOptions;
        private readonly IJwtService _jwtService;


        public TokenService(IOptions<AuthenticationOptions> options, IJwtService jwtService)
        {
            _authenticationOptions = options.Value;
            _jwtService = jwtService;
        }

        public async Task<TokenModel> GenerateTokens(User user, UserRefreshToken userRefreshToken)
        {
            var acessToken = await GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken(userRefreshToken);

            return new TokenModel(acessToken, refreshToken);
        }

        public ClaimsPrincipal ValidateRefreshToken(string token)
        {
            var key = Encoding.ASCII.GetBytes(_authenticationOptions.KeyRefreshToken);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidAudience = _authenticationOptions.Audience,
                ValidIssuer = _authenticationOptions.Issuer,
                IssuerSigningKey = new SymmetricSecurityKey(key),
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            JwtSecurityToken? jwtSecurityToken = securityToken as JwtSecurityToken;

            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.Aes256Gcm, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }



        private async Task<string> GenerateAccessToken(User user)
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Sid, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var key = await _jwtService.GetCurrentSigningCredentials();
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _authenticationOptions.Issuer,
                IssuedAt = DateTime.UtcNow,
                Audience = _authenticationOptions.Audience,
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_authenticationOptions.ExpireToken),
                SigningCredentials = key
            });

            return tokenHandler.WriteToken(token);
        }


        private string GenerateRefreshToken(UserRefreshToken userRefreshToken)
        {
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Sid, userRefreshToken.Id.ToString()),
                new Claim(ClaimTypes.Actor,  userRefreshToken.UserId.ToString()),
            };
            var key = Encoding.ASCII.GetBytes(_authenticationOptions.KeyRefreshToken);
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _authenticationOptions.Issuer,
                IssuedAt = DateTime.UtcNow,
                Audience = _authenticationOptions.Audience,
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_authenticationOptions.ExpireRefreshToken),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.Aes256Gcm)
            });
            return tokenHandler.WriteToken(token);
        }


    }
}
