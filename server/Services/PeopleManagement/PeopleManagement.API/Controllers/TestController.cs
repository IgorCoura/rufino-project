using Microsoft.AspNetCore.Authorization;
using PeopleManagement.API.Authorization;

namespace PeopleManagement.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController(ILogger<TestController> logger) : ControllerBase
    {
        [HttpGet]
        public IActionResult GetTest()
        {
            return Ok("It's ok. v1");
        }

        [HttpGet("auth")]
        [ProtectedResource("teste")]
        public IActionResult GetTestAuth()
        {
            return Ok("It's ok. Endpoint with Authorize");
        }

        [HttpGet("auth/role")]
        [ProtectedResource("teste", ["sc1", "sc2", "sc3"])]
        public IActionResult GetTestAuthRole()
        {
            return Ok("It's ok. Endpoint with Authorize just to admin");
        }

        [HttpGet("auth/role/{company}")]
        [ProtectedResource("teste", ["sc1", "sc2", "sc3"])]
        public IActionResult GetTestAuthRole(string company)
        {
            return Ok($"It's ok. Endpoint with Authorize just to admin - {company}");
        }
    }
}