using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuidManagement.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/Auth")]
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
        public async Task<ActionResult> PostAsync([FromBody] LoginModel model)
        {
            var token = await _authService.Login(model);
            return OkCustomResponse(token);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> GetAsync()
        {
            return OkCustomResponse("Api build-management esta funcionando");
        }
    }
}
