using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.RequireDocumentsCommands.CreateRequireSecurityDocuments;
using PeopleManagement.Application.Commands.DocumentCommands.CreateDocument;
using PeopleManagement.API.Authorization;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class RequireDocumentsController(ILogger<RequireDocumentsController> logger, IMediator mediator) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;

        [HttpPost]
        [ProtectedResource("RequireDocuments", "create")]
        public async Task<ActionResult<CreateRequireDocumentsResponse>> Create([FromRoute] Guid company, [FromBody] CreateRequireDocumentsModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateRequireDocumentsCommand, CreateRequireDocumentsResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.AssociationId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.AssociationId, request, requestId);

            return OkResponse(result);
        }

    }
}
