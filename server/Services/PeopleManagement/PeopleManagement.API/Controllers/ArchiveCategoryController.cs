using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.AddListenEvent;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.CreateArchiveCategory;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.EditDescriptionArchiveCategory;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.RemoveListenEvent;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Queries.ArchiveCategoryAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Events;
using static PeopleManagement.Application.Queries.ArchiveCategoryAggregate.ArchiveCategoryDtos;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class ArchiveCategoryController(ILogger<ArchiveCategoryController> logger, IMediator mediator, IArchiveCategoryQueries archiveCategoryQueries) : BaseController(logger)
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

        [HttpPut("description")]
        [ProtectedResource("ArchiveCategory", "create")]
        public async Task<ActionResult<EditDescriptionArchiveCategoryResponse>> EditDescription([FromRoute] Guid company, [FromBody] EditDescriptionArchiveCategoryModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<EditDescriptionArchiveCategoryCommand, EditDescriptionArchiveCategoryResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.ArchiveCategoryId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.ArchiveCategoryId, request, requestId);

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

        [HttpGet]
        [ProtectedResource("ArchiveCategory", "view")]
        public async Task<ActionResult<IEnumerable<ArchiveCategoryDTO>>> GetAllArchiveCategory([FromRoute] Guid company)
        {
            var result = await archiveCategoryQueries.GetAll(company);
            return OkResponse(result);
        }

        [HttpGet("event")]
        [ProtectedResource("ArchiveCategory", "view")]
        public ActionResult<IEnumerable<EmploymentContractType>> GetAllEvents([FromRoute] Guid company)
        {
            var result = RequestFilesEvent.GetAll();
            return OkResponse(result);
        }

    }
}
