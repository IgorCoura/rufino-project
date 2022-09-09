using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Purchase.MaterialPurchase;
using Microsoft.AspNetCore.Mvc;

namespace BuidManagement.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/MaterialPurchase")]
    public class MaterialPurchaseController : MainController
    {
        private readonly IMaterialPurchaseService _orderPurchaseService;

        public MaterialPurchaseController(IMaterialPurchaseService orderPurchaseService)
        {
            _orderPurchaseService = orderPurchaseService;
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateMaterialPurchaseModel model)
        {
            var result = await _orderPurchaseService.Create(model);
            return OkCustomResponse(result);
        }
    }
}
