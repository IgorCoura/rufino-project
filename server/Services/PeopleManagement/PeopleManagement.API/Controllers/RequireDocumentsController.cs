using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.RequireDocumentsCommands.CreateRequireSecurityDocuments;
using PeopleManagement.Application.Commands.DocumentCommands.CreateDocument;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class RequireDocumentsController(ILogger<CompanyController> logger, IMediator mediator) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;

        [HttpPost("create")]
        public async Task<ActionResult<CreateRequireDocumentsResponse>> Create([FromBody] CreateRequireDocumentsCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateRequireDocumentsCommand, CreateRequireDocumentsResponse>(request, requestId);

            SendingCommandLog(request.RoleId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.RoleId, request, requestId);

            return OkResponse(result);
        }

    }
}
