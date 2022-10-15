using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Models.Purchase.CreateMaterialPurchase;
using BuildManagement.Domain.Models.Purchase.MaterialPurchase;
using BuildManagement.Domain.Models.Purchase.MaterialReceive;
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
        public async Task<ActionResult> CreateMaterialPurchase([FromBody] CreateMaterialPurchaseRequest model)
        {
            var result = await _orderPurchaseService.CreateMaterialPurchase(model);
            return OkCustomResponse(result);
        }

        [HttpPost("PreAutorization/{id}")]
        public async Task<ActionResult> PreAutorization([FromRoute] Guid id)
        {
            await _orderPurchaseService.PreAuthorization(id);
            return OkCustomResponse();
        }

        [HttpPost("Autorization/{id}")]
        public async Task<ActionResult> Autorization([FromRoute] Guid id)
        {
            await _orderPurchaseService.Authorization(id);
            return OkCustomResponse();
        }

        [HttpPost("MaterialReceive")]
        public async Task<ActionResult> InsertMaterialReceive([FromBody] MaterialReceiveRequest model)
        {
            var result = await _orderPurchaseService.MaterialReceive(model);
            return OkCustomResponse(result);
        }


        [HttpGet("{Id}")]
        public async Task<ActionResult> Get([FromRoute] Guid id)
        {
            var result = await _orderPurchaseService.Get(id);
            return OkCustomResponse(result);
        }
    }
}
