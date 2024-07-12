using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.RequireSecurityDocumentsCommands.CreateRequireSecurityDocuments;
using PeopleManagement.Application.Commands.SecurityDocumentCommands.CreateDocument;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class RequireSecurityDocumentsController(ILogger<CompanyController> logger, IMediator mediator) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;

        [HttpPost("create")]
        public async Task<ActionResult<CreateRequireSecurityDocumentsResponse>> Create([FromBody] CreateRequireSecurityDocumentsCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateRequireSecurityDocumentsCommand, CreateRequireSecurityDocumentsResponse>(request, requestId);

            SendingCommandLog(request.RoleId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.RoleId, request, requestId);

            return OkResponse(result);
        }

    }
}
