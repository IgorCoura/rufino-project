using PeopleManagement.API.Authorization;
using static PeopleManagement.Application.Queries.Department.DepartmentDtos;
using static PeopleManagement.Application.Queries.Position.PositionDtos;
using PeopleManagement.Application.Queries.Position;
using PeopleManagement.Application.Commands.PositionCommands.CreatePosition;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.PositionCommands.EditPosition;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class PositionController(ILogger<DepartmentController> logger, IMediator mediator, IPositionQueries positionQueries) : BaseController(logger)
    {
        [HttpPost]
        [ProtectedResource("position", "create")]
        public async Task<ActionResult<CreatePositionResponse>> Create([FromRoute] Guid company, [FromBody] CreatePositionModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreatePositionCommand, CreatePositionResponse>(request.ToCommand(company), requestId);
            SendingCommandLog(request.Name, request, requestId);
            var result = await mediator.Send(command);
            CommandResultLog(result, request.Name, request, requestId);
            return OkResponse(result);
        }

        [HttpPut]
        [ProtectedResource("position", "edit")]
        public async Task<ActionResult<EditPositionResponse>> Edit([FromRoute] Guid company, [FromBody] EditPositionModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<EditPositionCommand, EditPositionResponse>(request.ToCommand(company), requestId);
            SendingCommandLog(request.Id, request, requestId);
            var result = await mediator.Send(command);
            CommandResultLog(result, request.Id, request, requestId);
            return OkResponse(result);
        }

        [HttpGet("{positionId}")]
        [ProtectedResource("position", "view")]
        public async Task<ActionResult<PositionSimpleDto>> GetById([FromRoute]Guid positionId, [FromRoute] Guid company)
        {
            var result = await positionQueries.GetById(positionId, company);
            return OkResponse(result);
        }

        [HttpGet("all/simple/{departmentId}")]
        [ProtectedResource("position", "view")]
        public async Task<ActionResult<PositionSimpleDto>> GetAllSimple([FromRoute] Guid departmentId, [FromRoute] Guid company)
        {
            var result = await positionQueries.GetAllSimple(departmentId, company);
            return OkResponse(result);
        }
    }
}
