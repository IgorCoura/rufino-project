using Commom.Domain.Exceptions;
using Identity.API.Application.Entities;
using Identity.API.Application.Errors;
using Identity.API.Application.Interfaces;
using Identity.API.Application.Model;
using Identity.API.Application.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Security.Jwt.Core.Interfaces;
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
            var acessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken(userRefreshToken);

            return new TokenModel(await acessToken, await refreshToken);
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

            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken 
                    || !jwtSecurityToken.Header.Alg.Equals("HS256"))
                {
                    throw new BadRequestException(IdentityErrors.RefreshTokenInvalid);
                }

                return principal;
            }
            catch
            {
                throw new BadRequestException(IdentityErrors.RefreshTokenInvalid);
            };
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


        private Task<string> GenerateRefreshToken(UserRefreshToken userRefreshToken)
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
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            });
            return Task.FromResult<string>(tokenHandler.WriteToken(token));
        }


    }
}
