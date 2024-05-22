using PeopleManagement.Application.Commands.CompanyCommands.CreateCompany;
using PeopleManagement.Application.Commands.DTO;
using PeopleManagement.Application.Commands.Identified;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class CompanyController : BaseController
    {
        private readonly IMediator _mediator;       

        public CompanyController(IMediator mediator, ILogger<CompanyController> logger) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<BaseDTO>> CreateCompany([FromBody] CreateCompanyCommand request, [FromHeader(Name = "x-requestid")] string requestId)
        {
           
            if (Guid.TryParse(requestId, out Guid guid) && guid != Guid.Empty)
            {
                var command = new IdentifiedCommand<CreateCompanyCommand, BaseDTO>(request, guid);

                SendingCommandLog(request.Cnpj, request, guid);

                var result = await _mediator.Send(command);

                CommandResultLog(result, request.Cnpj, request, guid);

                return OkResponse(result);
            }
            else
            {
                return BadRequestResponse("Invalid request Id");
            }

            
        }
    }
}
