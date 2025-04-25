using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.DocumentCommands.CreateDocument;
using PeopleManagement.Application.Commands.DocumentCommands.GeneratePdf;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocument;
using PeopleManagement.Application.Commands.DocumentCommands.GenerateDocumentToSign;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentToSign;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentSigned;
using System.Text.Json.Nodes;
using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.DocumentCommands.UpdateDocumentUnitDetails;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class DocumentController(ILogger<DocumentController> logger, IMediator mediator) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;

        [HttpPost]
        [ProtectedResource("Document", "create")]
        public async Task<ActionResult<CreateDocumentResponse>> Create([FromRoute]Guid company, [FromBody] CreateDocumentModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateDocumentCommand, CreateDocumentResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.DocumentId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.DocumentId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("DocumentUnit")]
        [ProtectedResource("Document", "edit")]
        public async Task<ActionResult<UpdateDocumentUnitDetailsResponse>> UpdateDocumentUnitDetails([FromRoute] Guid company, [FromBody] UpdateDocumentUnitDetailsModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<UpdateDocumentUnitDetailsCommand, UpdateDocumentUnitDetailsResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.DocumentUnitId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.DocumentUnitId, request, requestId);

            return OkResponse(result);
        }

        [HttpGet("{employeeId}/{documentId}/{documentUnitId}")]
        [ProtectedResource("Document", "view")]
        public async Task<ActionResult> GeneratePdf([FromRoute] Guid documentUnitId, [FromRoute] Guid documentId, [FromRoute] Guid employeeId, [FromRoute] Guid company, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var request = new GeneratePdfCommand(documentUnitId, documentId, employeeId, company);
            var command = new IdentifiedCommand<GeneratePdfCommand, GeneratePdfResponse>(request, requestId);

            SendingCommandLog(request.DocumentUnitId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.DocumentUnitId, request, requestId);

            return File(result.Pdf, "application/pdf", $"{result.Id}.pdf");
        }

        [HttpPost("send2sign")]
        [ProtectedResource("Document", "send")]
        public async Task<ActionResult<GenerateDocumentToSignResponse>> GeneratePdfToSign([FromRoute] Guid company, [FromBody] GenerateDocumentToSignModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var identifiedCommand = new IdentifiedCommand<GenerateDocumentToSignCommand, GenerateDocumentToSignResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.DocumentUnitId, request, requestId);

            var result = await _mediator.Send(identifiedCommand);

            CommandResultLog(result, request.DocumentUnitId, request, requestId);

            return result;
        }

        [HttpPost("insert")]
        [ProtectedResource("Document", "send")]
        public async Task<ActionResult<InsertDocumentResponse>> Insert(IFormFile formFile, [FromRoute] Guid company, [FromForm] InsertDocumentModel request,[FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var extension = Path.GetExtension(formFile.FileName);
            var stream = formFile.OpenReadStream();

            var command = new IdentifiedCommand<InsertDocumentCommand, InsertDocumentResponse>(request.ToCommand(company, extension, stream), requestId);

            SendingCommandLog(request.DocumentId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.DocumentId, request, requestId);

            return OkResponse(result);
        }

        [HttpPost("insert/send2sign")]
        [ProtectedResource("Document", "send")]
        public async Task<ActionResult<InsertDocumentToSignResponse>> InsertToSign(IFormFile formFile,[FromRoute] Guid company, [FromForm] InsertDocumentToSignModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var extension = Path.GetExtension(formFile.FileName);
            var stream = formFile.OpenReadStream();

            var command = request.ToCommand( stream, extension, company);
            var identifiedCommand = new IdentifiedCommand<InsertDocumentToSignCommand, InsertDocumentToSignResponse>(command, requestId);

            SendingCommandLog(request.DocumentUnitId, request, requestId);

            var result = await _mediator.Send(identifiedCommand);

            CommandResultLog(result, request.DocumentUnitId, request, requestId);

            return OkResponse(result);
        }


        [HttpPost("insert/signer")]
        [ProtectedResource("Document", "send")]
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
