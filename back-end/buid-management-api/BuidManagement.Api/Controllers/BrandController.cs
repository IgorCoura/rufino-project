using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Brand;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuidManagement.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/Brand")]
    public class BrandController : MainController
    {
        private readonly IBrandService _brandService;

        public BrandController(IBrandService brandService)
        {
            _brandService = brandService;
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateBrandModel model)
        {
            var result = await _brandService.Create(model);
            return OkCustomResponse(result);
        }
    }
}
