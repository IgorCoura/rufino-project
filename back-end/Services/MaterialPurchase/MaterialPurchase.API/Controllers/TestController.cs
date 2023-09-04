using Commom.API.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace MaterialPurchase.API.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TestController : MainController
    {
        [HttpGet]
        public ActionResult Get()
        {
            
            return OkCustomResponse("Funcionando");
        }
    }
}
