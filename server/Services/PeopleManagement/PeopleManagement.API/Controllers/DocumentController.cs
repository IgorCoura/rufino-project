using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Commands.DocumentCommands.CreateDocument;
using PeopleManagement.Application.Commands.DocumentCommands.GeneratePdf;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocument;
using PeopleManagement.Application.Commands.DocumentCommands.GenerateDocumentToSign;
using PeopleManagement.Application.Commands.DocumentCommands.InsertDocumentToSign;
using PeopleManagement.Application.Commands.DocumentCommands.ReceiveWebhookDocument;
using System.Text.Json.Nodes;
using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.DocumentCommands.UpdateDocumentUnitDetails;
using PeopleManagement.Application.Queries.Document;
using static PeopleManagement.Application.Queries.Document.DocumentDtos;
using PeopleManagement.Application.Queries.DocumentTemplate;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class DocumentController(ILogger<DocumentController> logger, IMediator mediator, IDocumentQueries documentQueries) : BaseController(logger)
    {
        private readonly IMediator _mediator = mediator;
        private readonly IDocumentQueries _documentQueries = documentQueries;

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

        [HttpGet("generate/{employeeId}/{documentId}/{documentUnitId}")]
        [ProtectedResource("Document", "view")]
        public async Task<ActionResult> GeneratePdf([FromRoute] Guid documentUnitId, [FromRoute] Guid documentId, [FromRoute] Guid employeeId, [FromRoute] Guid company, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var request = new GeneratePdfCommand(documentUnitId, documentId, employeeId, company);
            var command = new IdentifiedCommand<GeneratePdfCommand, GeneratePdfResponse>(request, requestId);

            SendingCommandLog(request.DocumentUnitId, request, requestId);

            var result = await _mediator.Send(command);

            CommandResultLog(result, request.DocumentUnitId, request, requestId);

            return File(result.Pdf, "application/pdf", $"doc_{result.Id.ToString().Substring(0,10)}.pdf");
        }

        [HttpPost("generate/send2sign")]
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
        [RequestSizeLimit(12_000_000)]
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
        [RequestSizeLimit(12_000_000)]
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


        [HttpPost("/api/v1/[controller]/webhook")]
        [ProtectedResource("Document", "webhook")]
        public async Task<ActionResult<ReceiveWebhookDocumentResponse>> ReceiveWebhook([FromBody] JsonNode request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new ReceiveWebhookDocumentCommand(request);
            var identifiedCommand = new IdentifiedCommand<ReceiveWebhookDocumentCommand, ReceiveWebhookDocumentResponse>(command, requestId);

            SendingCommandLog(request["external_id"], request, requestId);

            var result = await _mediator.Send(identifiedCommand);

            CommandResultLog(result, request["external_id"], request, requestId);

            return OkResponse(result);
        }

        [HttpGet("{employeeId}")]
        [ProtectedResource("Document", "view")]
        public async Task<ActionResult<IEnumerable<DocumentSimpleDto>>> GetAllSimple([FromRoute] Guid company, [FromRoute] Guid employeeId)
        {
            var result = await _documentQueries.GetAllSimple(employeeId, company);
            return OkResponse(result);
        }

        [HttpGet("{employeeId}/{id}")]
        [ProtectedResource("Document", "view")]
        public async Task<ActionResult<DocumentDto>> GetById([FromRoute] Guid company, [FromRoute] Guid employeeId, [FromRoute] Guid id)
        {
            var result = await _documentQueries.GetById(id, employeeId, company);
            return OkResponse(result);
        }

        [HttpGet("download/{employeeId}/{documentId}/{documentUnitId}")]
        [ProtectedResource("Document", "view")]
        public async Task<IActionResult> DownloadFile([FromRoute] Guid documentUnitId, [FromRoute] Guid documentId, [FromRoute] Guid employeeId,
            [FromRoute] Guid company)
        {
            var stream = await _documentQueries.DownloadDocumentUnit(documentUnitId, documentId, employeeId, company);  
            stream.Position = 0;
            return File(stream, "application/octet-stream", $"{documentUnitId}.zip");
        }


    }
}
