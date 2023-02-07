using Commom.API.Controllers;
using Commom.API.AuthorizationIds;
using Microsoft.AspNetCore.Mvc;
using MaterialControl.Domain.Consts;

namespace MaterialControl.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TesteController : MainController
    {
        
        public TesteController()
        {
        }

        [HttpGet]
        public ActionResult Get()
        {
            return OkCustomResponse("Funcionando");
        }

        [HttpGet("Auth")]
        [AuthorizationIdAttribute(MaterialControlAuthorizationIds.TesteAuth)]
        public ActionResult GetAuth()
        {
            return OkCustomResponse("Funcionando Auth");
        }
    }
}