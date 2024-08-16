using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.DocumentCommands.CreateDocument;
using PeopleManagement.Application.Commands.DocumentCommands.GeneratePdf;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocument;
using PeopleManagement.Application.Commands.DocumentCommands.GenerateDocumentToSign;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentToSign;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentSigned;
using System.Text.Json.Nodes;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class DocumentController(ILogger<CompanyController> logger, IMediator mediator) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;

        [HttpPost("create")]
        public async Task<ActionResult<CreateDocumentResponse>> Create([FromBody] CreateDocumentCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateDocumentCommand, CreateDocumentResponse>(request, requestId);

            SendingCommandLog(request.DocumentId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.DocumentId, request, requestId);

            return OkResponse(result);
        }

        [HttpGet("generate/{documentUnitId}/{documentId}/{employeeId}/{companyId}")]
        public async Task<ActionResult> GeneratePdf([FromRoute] Guid documentUnitId, [FromRoute] Guid documentId, [FromRoute] Guid employeeId, [FromRoute] Guid companyId, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var request = new GeneratePdfCommand(documentUnitId, documentId, employeeId, companyId);
            var command = new IdentifiedCommand<GeneratePdfCommand, GeneratePdfResponse>(request, requestId);

            SendingCommandLog(request.DocumentUnitId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.DocumentUnitId, request, requestId);

            return File(result.Pdf, "application/pdf", $"{result.Id}.pdf");
        }

        [HttpPost("generate/sign")]
        public async Task<ActionResult<GenerateDocumentToSignResponse>> GeneratePdfToSign([FromBody] GenerateDocumentToSignCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var identifiedCommand = new IdentifiedCommand<GenerateDocumentToSignCommand, GenerateDocumentToSignResponse>(request, requestId);

            SendingCommandLog(request.DocumentUnitId, request, requestId);

            var result = await _mediator.Send(identifiedCommand);

            CommandResultLog(result, request.DocumentUnitId, request, requestId);

            return result;
        }

        [HttpPost("insert")]
        public async Task<ActionResult<InsertDocumentResponse>> Insert(IFormFile formFile, [FromForm] Guid documentUnitId, [FromForm] Guid documentId, [FromForm] Guid employeeId, [FromForm] Guid companyId, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var extension = Path.GetExtension(formFile.FileName);
            var stream = formFile.OpenReadStream();

            var request = new InsertDocumentCommand(documentUnitId, documentId, employeeId, companyId, extension, stream);
            var command = new IdentifiedCommand<InsertDocumentCommand, InsertDocumentResponse>(request, requestId);

            SendingCommandLog(request.DocumentId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.DocumentId, request, requestId);

            return OkResponse(result);
        }

        [HttpPost("insert/sign")]
        public async Task<ActionResult<InsertDocumentToSignResponse>> InsertToSign(IFormFile formFile, [FromForm] InsertDocumentToSignModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var extension = Path.GetExtension(formFile.FileName);
            var stream = formFile.OpenReadStream();

            var command = request.ToCommand(stream, extension);
            var identifiedCommand = new IdentifiedCommand<InsertDocumentToSignCommand, InsertDocumentToSignResponse>(command, requestId);

            SendingCommandLog(request.DocumentUnitId, request, requestId);

            var result = await _mediator.Send(identifiedCommand);

            CommandResultLog(result, request.DocumentUnitId, request, requestId);

            return OkResponse(result);
        }


        [HttpPost("insert/signer")]
        public async Task<ActionResult<InsertDocumentSignedResponse>> InsertDocSigner([FromBody] JsonNode request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new InsertDocumentSignedCommand(request);
            var identifiedCommand = new IdentifiedCommand<InsertDocumentSignedCommand, InsertDocumentSignedResponse>(command, requestId);

            SendingCommandLog(request["external_id"], request, requestId);

            var result = await _mediator.Send(identifiedCommand);

            CommandResultLog(result, request["external_id"], request, requestId);

            return OkResponse(result);
        }


    }
}
