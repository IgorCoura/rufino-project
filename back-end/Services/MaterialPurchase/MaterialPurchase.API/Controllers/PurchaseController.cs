using MaterialPurchase.Domain.Consts;
using MaterialPurchase.Domain.Models.Request;
using Microsoft.AspNetCore.Mvc;
using MaterialPurchase.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;

namespace MaterialPurchase.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class PurchaseController : MainController
    {
        private readonly IPurchaseService _purchaseService;
        private readonly IAuthorizationService _authorizationService;

        public PurchaseController(IPurchaseService purchaseService, IAuthorizationService authorizationService)
        {
            _purchaseService = purchaseService;
            _authorizationService = authorizationService;
        }

        [HttpPost("Authorize")]
        public async Task<ActionResult> Authorize([FromBody] ReleasePurchaseRequest req)
        {
            return await VerifyAuthorize(_authorizationService, req, MaterialPurchaseAuthorizationId.AuthorizePurchase, async () =>
            {
                var result = await _purchaseService.AuthorizePurchase(Context, req);
                return OkCustomResponse(result);
            });
            
        }

        [HttpPost("Unlock")]
        public async Task<ActionResult> Unlock([FromBody] ReleasePurchaseRequest req)
        {
            return await VerifyAuthorize(_authorizationService, req, MaterialPurchaseAuthorizationId.UnlockPurchase, async () =>
            {
                var result = await _purchaseService.UnlockPurchase(Context, req);
                return OkCustomResponse(result);
            });
            
        }

        [HttpPost("Delivery/Confirm")]
        public async Task<ActionResult> ConfirmDeliveryDate([FromBody] ConfirmDeliveryDateRequest req)
        {
            return await VerifyAuthorize(_authorizationService, req, MaterialPurchaseAuthorizationId.ConfirmDeliveryDatePurchase, async () =>
            {
                var result = await _purchaseService.ConfirmDeliveryDate(Context, req);
                return OkCustomResponse(result);
            });
            
        }

        [HttpPost("Delivery/Receive")]
        public async Task<ActionResult> ReceiveDelivery([FromBody] ReceiveDeliveryRequest req)
        {
            return await VerifyAuthorize(_authorizationService, req, MaterialPurchaseAuthorizationId.ReceiveDeliveryPurchase, async () =>
            {
                var result = await _purchaseService.ReceiveDelivery(Context, req);
                return OkCustomResponse(result);
            });
            
        }

        [HttpPost("Cancel/BeforeAuthorize")]
        public async Task<ActionResult> CancelCreator([FromBody] CancelPurchaseRequest req)
        {
            
            var result = await _purchaseService.CancelPurchaseBeforeAuthorize(Context, req);
            return OkCustomResponse(result);

        }


        [HttpPost("Cancel/Admin")]
        public async Task<ActionResult> CancelAdmin([FromBody] CancelPurchaseRequest req)
        {

            return await VerifyAuthorize(_authorizationService, req, MaterialPurchaseAuthorizationId.CancelPurchaseAdmin, async () =>
            {
                var result = await _purchaseService.CancelPurchaseAdmin(Context, req);
                return OkCustomResponse(result);
            });
            

        }

    }
}
