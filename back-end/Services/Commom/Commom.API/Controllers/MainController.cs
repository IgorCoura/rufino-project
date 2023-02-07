using Commom.API.AuthorizationIds;
using Commom.Domain.BaseEntities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Commom.API.Controllers
{
    [ApiController]
    public class MainController : ControllerBase
    {
        public MainController()
        {
        }

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
            var result = errors.Select(error =>new 
            {
                ErrorCode = 0,
                ErrorMessage = error.Exception == null ? error.ErrorMessage : error.Exception.Message
            }).ToList();

            return BadRequest(new
            {
                success = false,
                erros = result
            });
        }

        protected Context Context
        {
            get
            {
                return new Context()
                {
                    User = new UserBase()
                    {
                        Id = Guid.Parse(User.FindFirstValue(ClaimTypes.Sid)),
                        Role = User.FindFirstValue(ClaimTypes.Role),
                        FunctionsId = User.FindAll(x => x.Type == AuthorizationIdOptions.POLICY_PREFIX).Select(x => x.Value).ToArray()
                    }
                };
            }
        }
    }
}
