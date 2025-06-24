using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.WorkplaceCommands.CreateWorkplace;
using PeopleManagement.Application.Commands.WorkplaceCommands.EditWorkplace;
using PeopleManagement.Application.Queries.Workplace;
using static PeopleManagement.Application.Queries.Workplace.WorkplaceDtos;

namespace PeopleManagement.API.Controllers
{


    [Route("api/v1/{company}/[controller]")]
    public class WorkplaceController(ILogger<WorkplaceController> logger, IMediator mediator, IWorkplaceQueries workplaceQueries) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;
        private readonly IWorkplaceQueries _workplaceQueries = workplaceQueries;

        [HttpPost]
        [ProtectedResource("Workplace", "create")]
        public async Task<ActionResult<CreateWorkplaceResponse>> Create([FromRoute] Guid company, [FromBody] CreateWorkplaceModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateWorkplaceCommand, CreateWorkplaceResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.Name, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.Name, request, requestId);

            return OkResponse(result);
        }

        [HttpPut]
        [ProtectedResource("Workplace", "edit")]
        public async Task<ActionResult<EditWorkplaceResponse>> Edit([FromRoute] Guid company, [FromBody] EditWorkplaceModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<EditWorkplaceCommand, EditWorkplaceResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.Id, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.Id, request, requestId);

            return OkResponse(result);
        }

        [HttpGet("{workplaceId}")]
        [ProtectedResource("Workplace", "view")]
        public async Task<ActionResult<WorkplaceDto>> GetById([FromRoute] Guid workplaceId, [FromRoute] Guid company)
        {
            var result = await _workplaceQueries.GetByIdAsync(workplaceId, company);
            return OkResponse(result);
        }

        [HttpGet]
        [ProtectedResource("Workplace", "view")]
        public async Task<ActionResult<WorkplaceDto>> GetAll([FromRoute] Guid company)
        {
            var result = await _workplaceQueries.GetAllAsync(company);
            return OkResponse(result);
        }
    }
}
