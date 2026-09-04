namespace BillPayment.API.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Sonda de vida. <see cref="AllowAnonymousAttribute"/> é explícito, não omissão: um health check
/// que exige token deixa de responder exatamente quando o Keycloak cai — que é quando alguém
/// mais precisa saber se a API está de pé.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { Status = "Running", Service = "BillPayment.API" });
    }
}
