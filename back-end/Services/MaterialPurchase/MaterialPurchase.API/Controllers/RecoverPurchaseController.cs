﻿using Commom.API.Controllers;
using Commom.API.FunctionIdAuthorization;
using MaterialPurchase.Domain.Consts;
using MaterialPurchase.Domain.Interfaces;
using MaterialPurchase.Domain.Models.Request;
using Microsoft.AspNetCore.Mvc;

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
        [FunctionIdAuthorize(MaterialPurchaseAuthorizationId.GetPurchaseSimple)]
        public async Task<ActionResult> SimpleGet([FromRoute] Guid id)
        {
            var result = await _recoverPurchaseService.SimpleRecover(id);
            return OkCustomResponse(result);
        }

        [HttpGet("Material/{id}")]
        [FunctionIdAuthorize(MaterialPurchaseAuthorizationId.GetPurchaseWithMaterial)]
        public async Task<ActionResult> GetWithMaterial([FromRoute] Guid id)
        {
            var result = await _recoverPurchaseService.RecoverPurchaseWithMaterials(id);
            return OkCustomResponse(result);
        }

        [HttpGet("Complete/{id}")]
        [FunctionIdAuthorize(MaterialPurchaseAuthorizationId.GetPurchaseComplete)]
        public async Task<ActionResult> CompleteGet([FromRoute] Guid id)
        {
            var result = await _recoverPurchaseService.RecoverPurchaseComplete(id);
            return OkCustomResponse(result);
        }

        [HttpGet("Complete")]
        [FunctionIdAuthorize(MaterialPurchaseAuthorizationId.GetAllPurchaseComplete)]
        public async Task<ActionResult> CompleteGetAll()
        {
            var result = await _recoverPurchaseService.RecoverAllPurchaseComplete();
            return OkCustomResponse(result);
        }

        [HttpGet("Material")]
        [FunctionIdAuthorize(MaterialPurchaseAuthorizationId.GetAllPurchaseWithMaterial)]
        public async Task<ActionResult> GetAllWithMaterial()
        {
            var result = await _recoverPurchaseService.RecoverAllPurchaseWithMaterials();
            return OkCustomResponse(result);
        }

        [HttpGet("Simple")]
        [FunctionIdAuthorize(MaterialPurchaseAuthorizationId.GetAllPurchaseSimple)]
        public async Task<ActionResult> SimpleGetAll()
        {
            var result = await _recoverPurchaseService.SimpleRecoverAll();
            return OkCustomResponse(result);
        }

    }
}