namespace PeopleManagement.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController(ILogger<TestController> logger) : ControllerBase
    {

        [HttpGet]
        public IActionResult GetTest()
        {
            return Ok("It's ok");
        }
    }
}