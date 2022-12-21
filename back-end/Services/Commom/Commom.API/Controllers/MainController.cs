using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Commom.API.Controllers
{
    [ApiController]
    public class MainController : ControllerBase
    {
        protected ActionResult OkCustomResponse(object? result = null)
        {
            return Ok(new
            {
                success = true,
                data = result
            });
        }

        protected ActionResult BadCustomResponse(ModelStateDictionary modelState)
        {
            var errors = modelState.Values.SelectMany(e => e.Errors);
            var result = errors.Select(error =>
            {
                return error.Exception == null ? error.ErrorMessage : error.Exception.Message;
            }).ToList();

            return BadRequest(new
            {
                success = false,
                erros = result
            });
        }
    }
}
