﻿using Commom.API.Controllers;
using Commom.API.AuthorizationIds;
using MaterialPurchase.Domain.Consts;
using MaterialPurchase.Domain.Models.Request;
using Microsoft.AspNetCore.Mvc;
using MaterialPurchase.Domain.Interfaces.Services;

namespace MaterialPurchase.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class PurchaseController : MainController
    {
        private readonly IPurchaseService _purchaseService;

        public PurchaseController(IPurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }

        [HttpPost("Authorize")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.AuthorizePurchase)]
        public async Task<ActionResult> Authorize([FromBody] PurchaseRequest req)
        {
            var result = await _purchaseService.AuthorizePurchase(Context, req);
            return OkCustomResponse(result);
        }

        [HttpPost("Unlock")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.UnlockPurchase)]
        public async Task<ActionResult> Unlock([FromBody] PurchaseRequest req)
        {
            var result = await _purchaseService.UnlockPurchase(Context, req);
            return OkCustomResponse(result);
        }

        [HttpPost("Delivery/Confirm")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.ConfirmDeliveryDatePurchase)]
        public async Task<ActionResult> ConfirmDeliveryDate([FromBody] ConfirmDeliveryDateRequest req)
        {
            var result = await _purchaseService.ConfirmDeliveryDate(Context, req);
            return OkCustomResponse(result);
        }

        [HttpPost("Delivery/Receive")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.ReceiveDeliveryPurchase)]
        public async Task<ActionResult> ReceiveDelivery([FromBody] ReceiveDeliveryRequest req)
        {
            var result = await _purchaseService.ReceiveDelivery(Context, req);
            return OkCustomResponse(result);
        }

        [HttpPost("Cancel/BeforeAuthorize")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.CancelPurchaseBeforeAuthoriz)]
        public async Task<ActionResult> CancelCreator([FromBody] CancelPurchaseRequest req)
        {
            var result = await _purchaseService.CancelPurchaseBeforeAuthorize(Context, req);
            return OkCustomResponse(result);
        }

        [HttpPost("Cancel/DuringAuthorize")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.CancelPurchaseDuringAuthorize)]
        public async Task<ActionResult> CancelClient([FromBody] CancelPurchaseRequest req)
        {
            var result = await _purchaseService.CancelPurchaseDuringAuthorize(Context, req);
            return OkCustomResponse(result);
        }

        [HttpPost("Cancel/AfterAuthorize")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.CancelPurchaseAfterAuthorize)]
        public async Task<ActionResult> CancelAdmin([FromBody] CancelPurchaseRequest req)
        {
            var result = await _purchaseService.CancelPurchaseAfterAuthorize(Context, req);
            return OkCustomResponse(result);
        }

    }
}
