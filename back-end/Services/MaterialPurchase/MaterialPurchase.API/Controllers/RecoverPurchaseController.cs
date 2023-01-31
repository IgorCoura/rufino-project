using Commom.API.Controllers;
using MaterialPurchase.Domain.Interfaces;
using MaterialPurchase.Domain.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace MaterialPurchase.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class RecoverPurchaseController: MainController
    {
        private readonly IRecoverPurchaseService _recoverPurchaseService;

        public RecoverPurchaseController(IRecoverPurchaseService recoverPurchaseService)
        {
            _recoverPurchaseService = recoverPurchaseService;
        }

        [HttpGet("Simple")]
        public async Task<ActionResult> SimpleGet(PurchaseRequest req)
        {
            var result = await _recoverPurchaseService.SimpleRecover(req);
            return OkCustomResponse(result);
        }

        [HttpGet("Material")]
        public async Task<ActionResult> GetWithMaterial(PurchaseRequest req)
        {
            var result = await _recoverPurchaseService.RecoverPurchaseWithMaterials(req);
            return OkCustomResponse(result);
        }

        [HttpGet("Complete/{id}")]
        public async Task<ActionResult> CompleteGet([FromRoute] Guid id)
        {
            var result = await _recoverPurchaseService.RecoverPurchaseComplete(new PurchaseRequest(id));
            return OkCustomResponse(result);
        }

        [HttpGet("Complete")]
        public async Task<ActionResult> CompleteGetAll()
        {
            var result = await _recoverPurchaseService.RecoverPurchaseAllComplete();
            return OkCustomResponse(result);
        }
    }
}
