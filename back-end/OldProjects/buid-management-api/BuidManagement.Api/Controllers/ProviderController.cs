using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Provider;
using Microsoft.AspNetCore.Mvc;

namespace BuidManagement.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/Provider")]
    public class ProviderController : MainController
    {
        private readonly IProviderService _providerService;

        public ProviderController(IProviderService providerService)
        {
            _providerService = providerService;
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody]CreateProviderModel model)
        {

            var result = await _providerService.Create(model);
            return OkCustomResponse(result);
        }
    }
}
