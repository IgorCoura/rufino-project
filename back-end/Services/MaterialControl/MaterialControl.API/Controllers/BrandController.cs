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
    public class BrandController : BaseController
    {
        private readonly IBrandService _brandService;

        public BrandController(IBrandService brandService)
        {
            _brandService = brandService;
        }

        [HttpPost]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.CreateBrand)]
        public async Task<ActionResult> Create([FromBody] CreateBrandRequest req)
        {
            var result = await _brandService.Create(Context, req);
            return OkCustomResponse(result);
        }

        [HttpPut]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.UpdateBrand)]
        public async Task<ActionResult> Update([FromBody] BrandRequest req)
        {
            var result = await _brandService.Update(Context, req);
            return OkCustomResponse(result);
        }

        [HttpDelete("{id}")]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.DeleteBrand)]
        public async Task<ActionResult> Delete([FromRoute] Guid id)
        {
            await _brandService.Delete(Context, id);
            return OkCustomResponse();
        }

        [HttpGet("{id}")]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.RecoverBrand)]
        public async Task<ActionResult> Recover([FromRoute] Guid id)
        {
            var result = await _brandService.Recover(id);
            return OkCustomResponse(result);
        }

        [HttpGet]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.RecoverAllBrand)]
        public async Task<ActionResult> RecoverAll()
        {
            var result = await _brandService.RecoverAll();
            return OkCustomResponse(result);
        }
    }
}
