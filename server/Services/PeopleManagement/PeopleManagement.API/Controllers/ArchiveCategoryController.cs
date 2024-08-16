using PeopleManagement.Application.Commands.ArchiveCategoryCommands.AddListenEvent;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.CreateArchiveCategory;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.RemoveListenEvent;
using PeopleManagement.Application.Commands.Identified;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class ArchiveCategoryController(ILogger<CompanyController> logger, IMediator mediator) : BaseController(logger)
    {
        [HttpPost("Create")]
        public async Task<ActionResult<CreateArchiveCategoryResponse>> Create([FromBody] CreateArchiveCategoryCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateArchiveCategoryCommand, CreateArchiveCategoryResponse>(request, requestId);

            SendingCommandLog(request.Name, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.Name, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("listenevent/add")]
        public async Task<ActionResult<AddListenEventResponse>> AddListenEvent([FromBody] AddListenEventCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<AddListenEventCommand, AddListenEventResponse>(request, requestId);

            SendingCommandLog(request.ArchiveCategoryId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.ArchiveCategoryId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("listenevent/remove")]
        public async Task<ActionResult<RemoveListenEventResponse>> RemoveListenEvent([FromBody] RemoveListenEventCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<RemoveListenEventCommand, RemoveListenEventResponse>(request, requestId);

            SendingCommandLog(request.ArchiveCategoryId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.ArchiveCategoryId, request, requestId);

            return OkResponse(result);
        }

    }
}
