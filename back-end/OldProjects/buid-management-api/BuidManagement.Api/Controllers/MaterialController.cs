using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Material;
using Microsoft.AspNetCore.Mvc;

namespace BuidManagement.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/Material")]
    public class MaterialController : MainController
    {
        private readonly IMaterialService _materialService;

        public MaterialController(IMaterialService materialService)
        {
            _materialService = materialService;
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateMaterialModel model)
        {
            var result = await _materialService.Create(model);
            return OkCustomResponse(result);
        }
    }
}
