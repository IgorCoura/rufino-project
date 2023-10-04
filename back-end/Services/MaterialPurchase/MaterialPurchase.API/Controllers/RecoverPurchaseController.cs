using MaterialPurchase.Domain.Consts;
using Microsoft.AspNetCore.Mvc;
using MaterialPurchase.Domain.Interfaces.Services;
using MaterialPurchase.Domain.Entities;
using Commom.Auth.Authorization;
using MaterialPurchase.Service.Services.Modify;
using Microsoft.AspNetCore.Authorization;
using MaterialPurchase.Domain.Models;

namespace MaterialPurchase.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class RecoverPurchaseController: MainController
    {
        private readonly IRecoverPurchaseService _recoverPurchaseService;
        private readonly IAuthorizationService _authorizationService;


        public RecoverPurchaseController(IRecoverPurchaseService recoverPurchaseService, IAuthorizationService authorizationService)
        {
            _recoverPurchaseService = recoverPurchaseService;
            _authorizationService = authorizationService;
        }

        [HttpGet("Simple/{companyId}/{constructionId}/{id}")]
        public async Task<ActionResult> SimpleGet([FromRoute] Guid constructionId, [FromRoute] Guid companyId, [FromRoute] Guid id)
        {
            var model = new ModelBase(constructionId, companyId);
            return await VerifyAuthorize(_authorizationService, model, MaterialPurchaseAuthorizationId.GetPurchaseSimple, async () =>
            {
                var result = await _recoverPurchaseService.SimpleRecover(id);
                return OkCustomResponse(result);
            });            
        }

        [HttpGet("Material/{companyId}/{constructionId}/{id}")]
        public async Task<ActionResult> GetWithMaterial([FromRoute] Guid constructionId, [FromRoute] Guid companyId,  [FromRoute] Guid id)
        {
            var model = new ModelBase(constructionId, companyId);
            return await VerifyAuthorize(_authorizationService, model, MaterialPurchaseAuthorizationId.GetPurchaseWithMaterial, async () =>
            {
                var result = await _recoverPurchaseService.RecoverPurchaseWithMaterials(id);
                return OkCustomResponse(result);
            });
            
        }

        [HttpGet("Complete/{companyId}/{constructionId}/{id}")]
        public async Task<ActionResult> CompleteGet([FromRoute] Guid constructionId, [FromRoute] Guid companyId, [FromRoute] Guid id)
        {
            var model = new ModelBase(constructionId, companyId);
            return await VerifyAuthorize(_authorizationService, model, MaterialPurchaseAuthorizationId.GetPurchaseComplete, async () =>
            {
                try
                {
                    var result = await _recoverPurchaseService.RecoverPurchaseComplete(id);
                    return OkCustomResponse(result);
                }
                catch(Exception ex)
                {
                    throw ex;
                }
                
            });
            
        }

        [HttpGet("Complete/{companyId}/{constructionId}")]
        public async Task<ActionResult> CompleteGetAll([FromRoute] Guid constructionId, [FromRoute] Guid companyId)
        {
            var model = new ModelBase(constructionId, companyId);
            return await VerifyAuthorize(_authorizationService, model, MaterialPurchaseAuthorizationId.GetAllPurchaseComplete, async () =>
            {
                var result = await _recoverPurchaseService.RecoverAllPurchaseComplete();
                return OkCustomResponse(result);
            });
            
        }

        [HttpGet("Material/{companyId}/{constructionId}")]
        public async Task<ActionResult> GetAllWithMaterial([FromRoute] Guid constructionId, [FromRoute] Guid companyId)
        {
            var model = new ModelBase(constructionId, companyId);
            return await VerifyAuthorize(_authorizationService, model, MaterialPurchaseAuthorizationId.GetAllPurchaseWithMaterial, async () =>
            {
                var result = await _recoverPurchaseService.RecoverAllPurchaseWithMaterials();
                return OkCustomResponse(result);
            });
        }

        [HttpGet("Simple/{companyId}/{constructionId}")]
        public async Task<ActionResult> SimpleGetAll([FromRoute] Guid constructionId, [FromRoute] Guid companyId)
        {
            var model = new ModelBase(constructionId, companyId);
            return await VerifyAuthorize(_authorizationService, model, MaterialPurchaseAuthorizationId.GetAllPurchaseSimple, async () =>
            {
                var result = await _recoverPurchaseService.SimpleRecoverAll();
                return OkCustomResponse(result);
            });
            
        }

    }
}
