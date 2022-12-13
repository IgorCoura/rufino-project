using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuidManagement.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("/api/v{version:apiVersion}/User")]
    [Authorize]
    public class UserController : MainController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> Post([FromBody] CreateUserModel model)
        {
            var result = await _userService.Create(model);
            return OkCustomResponse(result);
        }
    }
}
