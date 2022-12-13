using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Construction;
using Microsoft.AspNetCore.Mvc;

namespace BuidManagement.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/Construction")]
    public class ConstructionController : MainController
    {
        private readonly IConstructionService _constructionService;

        public ConstructionController(IConstructionService constructionService)
        {
            _constructionService = constructionService;
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateConstructionModel model)
        {
            var result = await _constructionService.Create(model);
            return OkCustomResponse(result);
        }
    }
}
