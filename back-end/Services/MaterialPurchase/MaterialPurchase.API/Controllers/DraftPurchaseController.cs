using MaterialPurchase.Domain.Consts;
using MaterialPurchase.Domain.Models.Request;
using Microsoft.AspNetCore.Mvc;
using MaterialPurchase.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Commom.Auth.Authorization;

namespace MaterialPurchase.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DraftPurchaseController : MainController
    {
        private readonly IDraftPurchaseService _draftPurchaseService;
        private readonly IAuthorizationService _authorizationService;

        public DraftPurchaseController(IDraftPurchaseService draftPurchaseService, IAuthorizationService authorizationService)
        {
            _draftPurchaseService = draftPurchaseService;
            _authorizationService = authorizationService;
        }

        [HttpPost("Create")]
        public async Task<ActionResult> Create([FromBody] CreateDraftPurchaseRequest req)
        {
            return await VerifyAuthorize(_authorizationService, req, MaterialPurchaseAuthorizationId.CreatePurchase, async () =>
            {
                var result = await _draftPurchaseService.Create(Context, req);
                return OkCustomResponse(result);
            });

        }

        [HttpPost("Update")]
        public async Task<ActionResult> Update([FromBody] DraftPurchaseRequest req)
        {
            return await VerifyAuthorize(_authorizationService, req, MaterialPurchaseAuthorizationId.UpdatePurchase, async () =>
            {
                var result = await _draftPurchaseService.Update(Context, req);
                return OkCustomResponse(result);
            });
            
        }

        [HttpPost("Delete")]
        public async Task<ActionResult> Delete([FromBody] PurchaseRequest req)
        {
            return await VerifyAuthorize(_authorizationService, req, MaterialPurchaseAuthorizationId.DeletePurchase, async () =>
            {
                await _draftPurchaseService.Delete(Context, req);
                return OkCustomResponse();
            });
            
        }

        [HttpPost("Send")]
        public async Task<ActionResult> Send([FromBody] PurchaseRequest req)
        {
            return await VerifyAuthorize(_authorizationService, req, MaterialPurchaseAuthorizationId.SendPurchase, async () =>
            {
                await _draftPurchaseService.SendToAuthorization(Context, req);
                return OkCustomResponse();
            });
            
        }


    }
}
