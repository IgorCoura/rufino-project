using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };


    public WeatherForecastController()
    {
    }

    [HttpGet]
    public ActionResult Get()
    {
        return Ok(Enumerable.Range(1, 5).Select(index => Summaries[Random.Shared.Next(Summaries.Length)])
        .ToArray());
    }

    [HttpGet("auth")]
    [Authorize]
    public ActionResult GetAuth()
    {
        return Ok(Enumerable.Range(1, 5).Select(index => Summaries[Random.Shared.Next(Summaries.Length)])
        .ToArray());
    }


}
