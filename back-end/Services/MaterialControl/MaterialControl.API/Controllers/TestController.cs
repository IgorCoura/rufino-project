using Commom.API.Controllers;
using Microsoft.AspNetCore.Mvc;
using MaterialControl.Domain.Consts;
using Commom.Auth.Authorization;

namespace MaterialControl.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TestController : BaseController
    {
        
        public TestController()
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