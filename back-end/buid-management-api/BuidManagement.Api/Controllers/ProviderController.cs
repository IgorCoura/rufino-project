using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Provider;
using Microsoft.AspNetCore.Mvc;

namespace BuidManagement.Api.Controllers
{
    [Route("api/v1/Provider")]
    public class ProviderController : ControllerBase
    {
        private readonly IProviderService _providerService;

        public ProviderController(IProviderService providerService)
        {
            _providerService = providerService;
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody]CreateProviderModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _providerService.Create(model);
            return Ok(result);
        }
    }
}
