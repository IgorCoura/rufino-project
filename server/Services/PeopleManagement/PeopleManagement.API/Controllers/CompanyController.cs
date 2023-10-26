using PeopleManagement.Application.Commands.CreateCompany;
using PeopleManagement.Application.Commands.DTO;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CompanyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<BaseDTO>> CreateCompany([FromBody] CreateCompanyCommand createCompany, [FromHeader(Name = "x-requestid")] string requestId)
        {
            try
            {
                if (Guid.TryParse(requestId, out Guid guid) && guid != Guid.Empty)
                {
                    return Created("", await _mediator.Send(createCompany));
                }
                else
                {
                    return BadRequest("Invalid request Id");
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
