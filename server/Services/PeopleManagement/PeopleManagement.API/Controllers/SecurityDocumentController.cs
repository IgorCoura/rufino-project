using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.SecurityDocumentCommands.CreateDocument;
using PeopleManagement.Application.Commands.SecurityDocumentCommands.GeneratePdf;
using PeopleManagement.Application.Commands.SecurityDocumentCommands.InsertDocument;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Interfaces;
using PeopleManagement.Domain.SeedWord;
using PeopleManagement.Infra.Context;
using PeopleManagement.Infra.Repository;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class SecurityDocumentController(ILogger<CompanyController> logger, IMediator mediator) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;

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

        [HttpPost("insert")]
        public async Task<ActionResult<InsertDocumentResponse>> Insert(IFormFile formFile, [FromForm] Guid documentId, [FromForm] Guid securityDocumentId, [FromForm] Guid employeeId, [FromForm] Guid companyId, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var extension = Path.GetExtension(formFile.FileName);
            var stream = formFile.OpenReadStream();

            var request = new InsertDocumentCommand(documentId, securityDocumentId, employeeId, companyId, extension, stream);
            var command = new IdentifiedCommand<InsertDocumentCommand, InsertDocumentResponse>(request, requestId);

            SendingCommandLog(request.DocumentId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.DocumentId, request, requestId);

            return OkResponse(result);
        }
    }
}
