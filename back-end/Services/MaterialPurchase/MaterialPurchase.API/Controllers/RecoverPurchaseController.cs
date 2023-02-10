using Commom.API.Controllers;
using Commom.API.AuthorizationIds;
using MaterialPurchase.Domain.Consts;
using MaterialPurchase.Domain.Models.Request;
using Microsoft.AspNetCore.Mvc;
using MaterialPurchase.Domain.Interfaces.Services;

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

        [HttpGet("Simple/{id}")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.GetPurchaseSimple)]
        public async Task<ActionResult> SimpleGet([FromRoute] Guid id)
        {
            var result = await _recoverPurchaseService.SimpleRecover(id);
            return OkCustomResponse(result);
        }

        [HttpGet("Material/{id}")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.GetPurchaseWithMaterial)]
        public async Task<ActionResult> GetWithMaterial([FromRoute] Guid id)
        {
            var result = await _recoverPurchaseService.RecoverPurchaseWithMaterials(id);
            return OkCustomResponse(result);
        }

        [HttpGet("Complete/{id}")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.GetPurchaseComplete)]
        public async Task<ActionResult> CompleteGet([FromRoute] Guid id)
        {
            var result = await _recoverPurchaseService.RecoverPurchaseComplete(id);
            return OkCustomResponse(result);
        }

        [HttpGet("Complete")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.GetAllPurchaseComplete)]
        public async Task<ActionResult> CompleteGetAll()
        {
            var result = await _recoverPurchaseService.RecoverAllPurchaseComplete();
            return OkCustomResponse(result);
        }

        [HttpGet("Material")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.GetAllPurchaseWithMaterial)]
        public async Task<ActionResult> GetAllWithMaterial()
        {
            var result = await _recoverPurchaseService.RecoverAllPurchaseWithMaterials();
            return OkCustomResponse(result);
        }

        [HttpGet("Simple")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.GetAllPurchaseSimple)]
        public async Task<ActionResult> SimpleGetAll()
        {
            var result = await _recoverPurchaseService.SimpleRecoverAll();
            return OkCustomResponse(result);
        }

    }
}
