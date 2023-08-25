using Commom.API.Controllers;
using Commom.API.AuthorizationIds;
using MaterialPurchase.Domain.Consts;
using MaterialPurchase.Domain.Models.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaterialPurchase.Domain.Interfaces.Services;
using Commom.Domain.BaseEntities;
using Microsoft.AspNetCore.Authorization;

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
            var isAuthorize = await _authorizationService.AuthorizeAsync(User, req, new AuthorizationIdRequirement(MaterialPurchaseAuthorizationId.CreatePurchase));
            var result = await _draftPurchaseService.Create(Context, req);
            return OkCustomResponse(result);
        }

        [HttpPost("Update")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.UpdatePurchase)]
        public async Task<ActionResult> Update([FromBody] DraftPurchaseRequest req)
        {
            var result = await _draftPurchaseService.Update(Context, req);
            return OkCustomResponse(result);
        }

        [HttpPost("Delete")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.DeletePurchase)]
        public async Task<ActionResult> Delete([FromBody] PurchaseRequest req)
        {
            await _draftPurchaseService.Delete(Context, req);
            return OkCustomResponse();
        }

        [HttpPost("Send")]
        [AuthorizationIdAttribute(MaterialPurchaseAuthorizationId.SendPurchase)]
        public async Task<ActionResult> Send([FromBody] PurchaseRequest req)
        {
            await _draftPurchaseService.SendToAuthorization(Context, req);
            return OkCustomResponse();
        }


    }
}
