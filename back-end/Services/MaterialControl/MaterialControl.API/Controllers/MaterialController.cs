using Commom.API.Controllers;
using Commom.Auth.Authorization;
using MaterialControl.Domain.Consts;
using MaterialControl.Domain.Interfaces;
using MaterialControl.Domain.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace MaterialControl.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class MaterialController : BaseController
    {
        private readonly IMaterialService _materialService;

        public MaterialController(IMaterialService materialService)
        {
            _materialService = materialService;
        }

        [HttpPost]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.CreateMaterial)]
        public async Task<ActionResult> Create([FromBody] CreateMaterialRequest req)
        {
            var result = await _materialService.Create(Context, req);
            return OkCustomResponse(result);
        }

        [HttpPut]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.UpdateMaterial)]
        public async Task<ActionResult> Update([FromBody] MaterialRequest req)
        {
            var result = await _materialService.Update(Context, req);
            return OkCustomResponse(result);
        }

        [HttpDelete("{id}")]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.DeleteMaterial)]
        public async Task<ActionResult> Delete([FromRoute] Guid id)
        {
            await _materialService.Delete(Context, id);
            return OkCustomResponse();
        }

        [HttpGet("{id}")]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.RecoverMaterial)]
        public async Task<ActionResult> Recover([FromRoute] Guid id)
        {
            var result = await _materialService.Recover(id);
            return OkCustomResponse(result);
        }

        [HttpGet]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.RecoverAllMaterial)]
        public async Task<ActionResult> RecoverAll()
        {
            var result = await _materialService.RecoverAll();
            return OkCustomResponse(result);
        }
    }
}
