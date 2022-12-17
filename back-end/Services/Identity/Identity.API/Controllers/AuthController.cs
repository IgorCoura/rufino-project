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
    public class AuthController : ControllerBase
    {
        private readonly IJwtService _jwtService;

        public AuthController(IJwtService jwtService)
        {
            _jwtService = jwtService;
        }

        [HttpGet]
        public async Task<ActionResult>  Get()
        {
            var clains = new List<Claim>()
                {
                    new Claim(ClaimTypes.Sid, Guid.NewGuid().ToString())
                };
            var token = await GenerateToken(clains);
            return Ok(new
            {
                success = true,
                data = token
            });
        }

        private async Task<string> GenerateToken(IEnumerable<Claim> claims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = await _jwtService.GetCurrentSigningCredentials();
            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = "http://identity.api", // TODO: Automatizar Issuer
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(60),
                SigningCredentials = key,
            });
            return tokenHandler.WriteToken(token);
        }
    }
}
