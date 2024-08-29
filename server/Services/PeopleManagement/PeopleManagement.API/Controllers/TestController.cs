using Microsoft.AspNetCore.Authorization;

namespace PeopleManagement.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController(ILogger<TestController> logger) : ControllerBase
    {

        [HttpGet]
        public IActionResult GetTest()
        {
            return Ok("It's ok.");
        }

        [HttpGet("auth")]
        [Authorize]
        public IActionResult GetTestAuth()
        {
            return Ok("It's ok. Endpoint with Authorize");
        }

        [HttpGet("auth/role")]
        [Authorize]
        public IActionResult GetTestAuthRole()
        {
            return Ok("It's ok. Endpoint with Authorize just to admin");
        }
    }
}