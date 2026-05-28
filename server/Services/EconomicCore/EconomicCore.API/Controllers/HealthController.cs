using Microsoft.AspNetCore.Mvc;

namespace EconomicCore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { Status = "Running", Service = "EconomicCore.API" });
    }
}
