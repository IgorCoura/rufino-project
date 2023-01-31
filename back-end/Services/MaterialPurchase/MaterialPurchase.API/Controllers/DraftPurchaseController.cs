using Commom.API.Controllers;
using Commom.API.FunctionIdAuthorization;
using MaterialPurchase.Domain.Consts;
using MaterialPurchase.Domain.Interfaces;
using MaterialPurchase.Domain.Models.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaterialPurchase.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DraftPurchaseController : MainController
    {
        private readonly IDraftPurchaseService _draftPurchaseService;
        private readonly IConstructionRepository constructionRepository;

        public DraftPurchaseController(IDraftPurchaseService draftPurchaseService, IConstructionRepository constructionRepository)
        {
            _draftPurchaseService = draftPurchaseService;
            this.constructionRepository = constructionRepository;
        }

        [HttpPost("Create")]
        [FunctionIdAuthorizeAttribute(MaterialPurchaseAuthorizationId.CreatePurchase)]
        public async Task<ActionResult> Create([FromBody] CreateDraftPurchaseRequest req)
        {
            var result = await _draftPurchaseService.Create(Context(), req);
            return OkCustomResponse(result);
        }

        [HttpPost("Update")]
        [FunctionIdAuthorizeAttribute(MaterialPurchaseAuthorizationId.UpdatePurchase)]
        public async Task<ActionResult> Update([FromBody] DraftPurchaseRequest req)
        {
            var result = await _draftPurchaseService.Update(req);
            return OkCustomResponse(result);
        }

        [HttpPost("Delete")]
        [FunctionIdAuthorizeAttribute(MaterialPurchaseAuthorizationId.DeletePurchase)]
        public async Task<ActionResult> Delete([FromBody] PurchaseRequest req)
        {
            await _draftPurchaseService.Delete(req);
            return OkCustomResponse();
        }

        [HttpPost("Send")]
        [FunctionIdAuthorizeAttribute(MaterialPurchaseAuthorizationId.SendPurchase)]
        public async Task<ActionResult> Send([FromBody] PurchaseRequest req)
        {
            await _draftPurchaseService.SendToAuthorization(req);
            return OkCustomResponse();
        }


    }
}
