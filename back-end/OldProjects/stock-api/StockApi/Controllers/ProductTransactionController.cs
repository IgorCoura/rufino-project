using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockApi.Domain.Entities;
using StockApi.Domain.Exceptions;
using StockApi.Domain.Interfaces;
using StockApi.Domain.Models;

namespace StockApi.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ProductTransactionController : ControllerBase
    {
        private readonly IServiceProductTransaction _serviceTransaction;

        public ProductTransactionController(IServiceProductTransaction serviceTransaction)
        {
            _serviceTransaction = serviceTransaction;
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] List<CreateTransactionModel> transactions)
        {
            try
            {
                var resp = await _serviceTransaction.Insert(transactions); 
                return Ok(resp);    
            }
            catch(DomainException e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
