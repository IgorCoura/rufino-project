using Commom.API.AuthorizationIds;
using Commom.API.Controllers;
using MaterialControl.Domain.Consts;
using MaterialControl.Domain.Interfaces;
using MaterialControl.Domain.Models.Request;
using Microsoft.AspNetCore.Mvc;

namespace MaterialControl.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UnityController : MainController
    {
        private readonly IUnityService _unityService;

        public UnityController(IUnityService unityService)
        {
            _unityService = unityService;
        }

        [HttpPost]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.CreateUnity)]
        public async Task<ActionResult> Create([FromBody] CreateUnityRequest req)
        {
            var result = await _unityService.Create(Context, req);
            return OkCustomResponse(result);
        }

        [HttpPut]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.UpdateUnity)]
        public async Task<ActionResult> Update([FromBody] UnityRequest req)
        {
            var result = await _unityService.Update(Context, req);
            return OkCustomResponse(result);
        }

        [HttpDelete("{id}")]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.DeleteUnity)]
        public async Task<ActionResult> Delete([FromRoute] Guid id)
        {
            await _unityService.Delete(Context, id);
            return OkCustomResponse();
        }

        [HttpGet("{id}")]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.RecoverUnity)]
        public async Task<ActionResult> Recover([FromRoute] Guid id)
        {
            var result = await _unityService.Recover(id);
            return OkCustomResponse(result);
        }

        [HttpGet]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.RecoverAllUnity)]
        public async Task<ActionResult> RecoverAll()
        {
            var result = await _unityService.RecoverAll();
            return OkCustomResponse(result);
        }
    }
}
