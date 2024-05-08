using PeopleManagement.Application.Commands.DTO;
using PeopleManagement.Application.Commands.EmployeeCommands.CreateEmployee;
using PeopleManagement.Application.Commands.Identified;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class EmployeeController : BaseController
    {
        private readonly IMediator _mediator;
        public EmployeeController(ILogger<CompanyController> logger, IMediator mediator) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<BaseDTO>> Create([FromBody] CreateEmployeeCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
                var command = new IdentifiedCommand<CreateEmployeeCommand, BaseDTO>(request, requestId);

                SendingCommandLog(request.GetType().Name, nameof(request.Name), request.Name, request);

                var result = await _mediator.Send(command);

                CommandResultLog(result, request.GetType().Name, nameof(request.Name), request.Name, command);

                return OkResponse(result);

        }

    }
}
