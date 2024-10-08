﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockApi.Domain.Exceptions;
using StockApi.Domain.Interfaces;

namespace StockApi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IServiceProduct _serviceProduct;

        public ProductController(IServiceProduct serviceProduct)
        {
            _serviceProduct = serviceProduct;
        }

        [HttpGet]
        public IActionResult GetAll([FromQuery] DateTime modificationDate)
        {
            try
            {
                var objs = _serviceProduct.RecoverAll(modificationDate);
                return Ok(objs);
            }
            catch (DomainException e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            try
            {
                var objs = _serviceProduct.RecoverAll();
                return Ok(objs);
            }
            catch (DomainException e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
