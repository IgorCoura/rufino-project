using Microsoft.AspNetCore.Authorization;

namespace PeopleManagement.API.Controllers
{
    /// <summary>
    /// Sonda de vida. O <see cref="AllowAnonymousAttribute"/> é explícito, não omissão: um health
    /// check que exige token deixa de responder exatamente quando o Keycloak cai — que é quando
    /// alguém mais precisa saber se a API está de pé.
    /// </summary>
    /// <remarks>
    /// Existe desde que o fallback de autorização passou a exigir autenticação (2026-09-04). Até
    /// então a única rota anônima da API era o <c>GET /Test</c> do <c>TestController</c>, que
    /// nasceu como brinquedo de desenvolvimento e foi removido no mesmo passo.
    /// </remarks>
    [ApiController]
    [AllowAnonymous]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { Status = "Running", Service = "PeopleManagement.API" });
        }
    }
}
