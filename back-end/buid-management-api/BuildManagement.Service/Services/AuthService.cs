using BuildManagement.Domain.Exceptions;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Login;
using BuildManagement.Domain.Options;
using BuildManagement.Service.Utils;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly TokenGeneratorOptions _options;

        public AuthService(IUserRepository userRepository, IOptions<TokenGeneratorOptions> options)
        {
            _userRepository = userRepository;
            _options = options.Value;
        }
        public async Task<string> Login(LoginModel model)
        {
            var user = await _userRepository.FirstAsync(filter: x => x.UserName == model.UserName) ?? throw new NotFoundRequestException("Username ou senha incorreta.", nameof(model));


            if (!PasswordHasher.Verify(model.Password, user.Password))
                throw new BadRequestException("Username ou senha incorreta.", nameof(model));

            var clains = new List<Claim>()
                {
                    new Claim(ClaimTypes.Sid, user.Id.ToString()),
                };

            return await GenerateToken(clains);

        }

        public Task<string> GenerateToken(IEnumerable<Claim> claims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_options.SecurityKey);
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            });
            return Task.FromResult(tokenHandler.WriteToken(token));
        }
    }
}
