using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockApi.Domain.Exceptions;

namespace StockApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Register()
        {
            try
            {
                return Ok();
            }
            catch (DomainException ex)
            {
                
                return BadRequest(ex.Message);
            }
        }
    }
}
