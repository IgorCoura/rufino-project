
using PeopleManagement.Application.Commands.EmployeeCommands.AlterMilitarDocumentEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.SecurityDocumentCommands.CreateDocument;
using PeopleManagement.Application.Commands.SecurityDocumentCommands.GeneratePdf;
using System.Net;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class SecurityDocumentController : BaseController
    {
        private readonly IMediator _mediator;
        public SecurityDocumentController(ILogger<CompanyController> logger, IMediator mediator) : base(logger)
        {
            _mediator = mediator;
        }

        [HttpPost("create")]
        public async Task<ActionResult<CreateDocumentResponse>> Create([FromBody] CreateDocumentCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateDocumentCommand, CreateDocumentResponse>(request, requestId);

            SendingCommandLog(request.SecurityDocumentId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.SecurityDocumentId, request, requestId);

            return OkResponse(result);
        }

        [HttpGet("pdf/{documentId}/{securityDocumentId}/{employeeId}/{companyId}")]
        public async Task<ActionResult> GeneratePdf([FromRoute] Guid documentId, [FromRoute] Guid securityDocumentId, [FromRoute] Guid employeeId, [FromRoute] Guid companyId, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var request = new GeneratePdfCommand(documentId, securityDocumentId, employeeId, companyId);
            var command = new IdentifiedCommand<GeneratePdfCommand, GeneratePdfResponse>(request, requestId);

            SendingCommandLog(request.DocumentId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.DocumentId, request, requestId);

            return File(result.Pdf, "application/pdf", $"{result.Id}.pdf");
        }

    }
}
