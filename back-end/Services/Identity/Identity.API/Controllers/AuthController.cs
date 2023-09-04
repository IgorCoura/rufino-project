using Commom.API.Controllers;
using Identity.API.Application.Interfaces;
using Identity.API.Application.Model;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("SignIn")]
        public async Task<ActionResult> Post([FromBody] LoginModel loginModel)
        {
            if (!ModelState.IsValid) return BadCustomResponse(ModelState);

            var tokens = await _authService.SingIn(loginModel);

            return OkCustomResponse(tokens);
        }

        [HttpPost("Refresh")]
        public async Task<ActionResult> PostRefresh([FromBody] RefreshTokenModel refreshTokenModel)
        {
            if (!ModelState.IsValid) return BadCustomResponse(ModelState);

            var tokens = await _authService.RefreshToken(refreshTokenModel.RefreshToken);

            return OkCustomResponse(tokens);
        }

        [HttpPost("SignOut")]
        public async Task<ActionResult> PostSingOut([FromBody] RefreshTokenModel refreshTokenModel)
        {
            if (!ModelState.IsValid) return BadCustomResponse(ModelState);

            await _authService.SingOutLocal(refreshTokenModel.RefreshToken);

            return OkCustomResponse();
        }

        [HttpPost("SignOut/All")]
        public async Task<ActionResult> PostSingOutAll([FromBody] RefreshTokenModel refreshTokenModel)
        {
            if (!ModelState.IsValid) return BadCustomResponse(ModelState);

            await _authService.SingOutAll(refreshTokenModel.RefreshToken);

            return OkCustomResponse();
        }

    }
}
