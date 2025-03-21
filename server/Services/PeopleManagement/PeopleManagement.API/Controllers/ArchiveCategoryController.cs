using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.AddListenEvent;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.CreateArchiveCategory;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.RemoveListenEvent;
using PeopleManagement.Application.Commands.Identified;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class ArchiveCategoryController(ILogger<ArchiveCategoryController> logger, IMediator mediator) : BaseController(logger)
    {
        [HttpPost]
        [ProtectedResource("ArchiveCategory", "create")]
        public async Task<ActionResult<CreateArchiveCategoryResponse>> Create([FromRoute] Guid company, [FromBody] CreateArchiveCategoryModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateArchiveCategoryCommand, CreateArchiveCategoryResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.Name, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.Name, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("event/add")]
        [ProtectedResource("ArchiveCategory", "edit")]
        public async Task<ActionResult<AddListenEventResponse>> AddEvents([FromRoute] Guid company, [FromBody] AddListenEventModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AddListenEventCommand, AddListenEventResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.ArchiveCategoryId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.ArchiveCategoryId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("event/remove")]
        [ProtectedResource("ArchiveCategory", "edit")]
        public async Task<ActionResult<RemoveListenEventResponse>> RemoveEvents([FromRoute] Guid company, [FromBody] RemoveListenEventModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<RemoveListenEventCommand, RemoveListenEventResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.ArchiveCategoryId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.ArchiveCategoryId, request, requestId);

            return OkResponse(result);
        }

    }
}
