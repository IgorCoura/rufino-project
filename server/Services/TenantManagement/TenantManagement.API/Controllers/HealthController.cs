namespace TenantManagement.API.Controllers;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy" });
}
