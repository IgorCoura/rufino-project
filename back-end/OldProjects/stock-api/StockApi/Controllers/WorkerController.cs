using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockApi.Domain.Exceptions;
using StockApi.Domain.Interfaces;

namespace StockApi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class WorkerController : ControllerBase
    {
        private readonly IServiceWorker _serviceWorker;

        public WorkerController(IServiceWorker serviceWorker)
        {
            _serviceWorker = serviceWorker;
        }

        [HttpGet]
        public IActionResult GetAll([FromQuery] DateTime modificationDate)
        {
            try
            {
                var objs = _serviceWorker.RecoverAll(modificationDate);
                return Ok(objs);
            }
            catch(DomainException e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
