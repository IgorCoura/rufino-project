using System.Security.Cryptography;
using Authenticate.Api.Interfaces;
using Authenticate.Api.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Authenticate.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class AuthController : MainController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> Post([FromBody] LoginModel model)
        {
            var token = await _authService.Login(model);
            return OkCustomResponse(token);
            
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> Get()
        {
            return OkCustomResponse("A Bagaca ta funcionando, sem Auth");
            
        }

        [HttpGet("get")]        
        public async Task<ActionResult> GetAuth()
        {
            return OkCustomResponse("A Bagaca ta funcionando");
            
        }

    }
}
