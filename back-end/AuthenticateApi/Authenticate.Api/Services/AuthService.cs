
using System.Security.Claims;
using System.Text;
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

        public AuthService(
            //IUserRepository userRepository, 
            IOptions<TokenGeneratorOptions> options)
        {
            //_userRepository = userRepository;
            _options = options.Value;
        }

        public async Task<string> Login(LoginModel model)
        {
            //var user = await _userRepository.FirstAsync(filter: x => x.Username == model.Username) ?? throw new NotFoundRequestException("Username ou senha incorreta.");

            var user = new
            {
                Id = Guid.NewGuid(),
                Email = model.Username,
                Name = "Teste",
                Role = "Admin"
            };

            //if (!PasswordHasher.Verify(model.Password, user.Password))
            //    throw new BadRequestException(nameof(model.Password), "Username ou senha incorreta.");

            var clains = new List<Claim>()
                {
                    new Claim(ClaimTypes.Sid, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, user.Role),
                };

            return await GenerateToken(clains);

        }

        private Task<string> GenerateToken(IEnumerable<Claim> claims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            byte[] key = Encoding.ASCII.GetBytes(s: _options.SecurityKey
                ?? throw new ArgumentNullException("SecurityKey not can be null.", nameof(_options.SecurityKey)));
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
