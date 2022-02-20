using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockApi.Domain.Exceptions;
using StockApi.Domain.Interfaces;

namespace StockApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IServiceProduct _serviceProduct;

        public ProductController(IServiceProduct serviceProduct)
        {
            _serviceProduct = serviceProduct;
        }

        [HttpGet]
        public IActionResult GetAll([FromQuery] DateTime modificationDate)
        {
            try
            {
                var objs = _serviceProduct.RecoverAll(modificationDate);
                return Ok(objs);
            }
            catch (DomainException e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
