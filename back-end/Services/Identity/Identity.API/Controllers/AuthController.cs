using Commom.API.Controllers;
using Identity.API.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using NetDevPack.Security.Jwt.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : MainController
    {
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _config;

        public AuthController(IJwtService jwtService, IConfiguration config)
        {
            _jwtService = jwtService;
            _config = config;
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] LoginModel loginModel)
        {
            if (!ModelState.IsValid) return BadCustomResponse(ModelState);

            var clains = new List<Claim>()
                {
                    new Claim(ClaimTypes.Sid, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, loginModel.UserName),
                };
            var token = await GenerateToken(clains);

            return OkCustomResponse(token);
        }

        private async Task<string> GenerateToken(IEnumerable<Claim> claims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var key = await _jwtService.GetCurrentSigningCredentials();
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _config["Jwt:Issuer"],
                IssuedAt = DateTime.UtcNow,
                Audience = _config["Jwt:Audience"],
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(60),
                SigningCredentials = key
            });
            return tokenHandler.WriteToken(token);
        }
    }
}
