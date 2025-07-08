using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.EditDocumentTemplate;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.InsertDocumentTemplate;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Application.Queries.DocumentTemplate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Services.Services.RecoverInfoToDocument;
using System.Text.Json.Nodes;
using static PeopleManagement.Application.Queries.Base.BaseDtos;
using static PeopleManagement.Application.Queries.DocumentTemplate.DocumentTemplateDtos;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class DocumentTemplateController(ILogger<DocumentTemplateController> logger, IMediator mediator, IDocumentTemplateQueries documentTemplateQueries) : BaseController(logger)
    {
        [HttpPost]
        [ProtectedResource("DocumentTemplate", "create")]
        public async Task<ActionResult<CreateDocumentTemplateResponse>> Create([FromBody] CreateDocumentTemplateModel request, [FromRoute] Guid company, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateDocumentTemplateCommand, CreateDocumentTemplateResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(company, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, company, request, requestId);

            return OkResponse(result);
        }

        [HttpPut]
        [ProtectedResource("DocumentTemplate", "edit")]
        public async Task<ActionResult<EditDocumentTemplateResponse>> Edit([FromBody] EditDocumentTemplateModel request, [FromRoute] Guid company, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<EditDocumentTemplateCommand, EditDocumentTemplateResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(command.Id, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, command.Id, request, requestId);

            return OkResponse(result);
        }

        [HttpPost("Upload")]
        [ProtectedResource("DocumentTemplate", "send")]
        [RequestSizeLimit(12_000_000)]
        public async Task<ActionResult<InsertDocumentTemplateResponse>> Insert(IFormFile formFile, [FromForm] Guid id, [FromRoute] Guid company, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var stream = formFile.OpenReadStream();
            var request = new InsertDocumentTemplateCommand(id, company, formFile.FileName, stream);
            var command = new IdentifiedCommand<InsertDocumentTemplateCommand, InsertDocumentTemplateResponse>(request, requestId);

            SendingCommandLog(request.DocumentTemplateId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.DocumentTemplateId, request, requestId);

            return OkResponse(result);
        }

        [HttpGet]
        [ProtectedResource("DocumentTemplate", "view")]
        public async Task<ActionResult<IEnumerable<DocumentTemplateDto>>> GetAll([FromRoute] Guid company)
        {
            var result = await documentTemplateQueries.GetAll(company);
            return OkResponse(result);
        }

        [HttpGet("{documentTemplateId}")]
        [ProtectedResource("DocumentTemplate", "view")]
        public async Task<ActionResult<DocumentTemplateDto>> GetById([FromRoute] Guid company, [FromRoute] Guid documentTemplateId)
        {
            var result = await documentTemplateQueries.GetById(company, documentTemplateId);
            return OkResponse(result);
        }

        [HttpGet("Simple")]
        [ProtectedResource("DocumentTemplate", "view")]
        public async Task<ActionResult<IEnumerable<DocumentTemplateSimpleDto>>> GetAllSimple([FromRoute] Guid company)
        {
            var result = await documentTemplateQueries.GetAllSimple(company);
            return OkResponse(result);
        }


        [HttpGet("TypeSignature")]
        [ProtectedResource("DocumentTemplate", "view")]
        public ActionResult<IEnumerable<TypeSignature>> GetAllTypeSignatures([FromRoute] Guid company)
        {
            var result = TypeSignature.GetAll<TypeSignature>();
            return OkResponse(result);
        }

        [HttpGet("RecoverDataType")]
        [ProtectedResource("DocumentTemplate", "view")]
        public ActionResult<IEnumerable<EnumerationDto>> GetAllRecoverDataType([FromRoute] Guid company)
        {
            var result = RecoverDataType.GetAll<RecoverDataType>();
            var dtos = result.Select(x => new EnumerationDto { Id = x.Id, Name = x.Name });
            return OkResponse(dtos);
        }

        [HttpGet("HasFile/{documentTemplateId}")]
        [ProtectedResource("DocumentTemplate", "view")]
        public async Task<ActionResult<bool>> HasFile([FromRoute] Guid company, [FromRoute] Guid documentTemplateId)
        {
            var result = await documentTemplateQueries.HasFile(documentTemplateId, company);
            return OkResponse(result);
        }

        [HttpGet("Download/{documentTemplateId}")]
        [ProtectedResource("DocumentTemplate", "view")]
        public async Task<IActionResult> DownloadFile([FromRoute] Guid company, [FromRoute] Guid documentTemplateId)
        {
            var stream = await documentTemplateQueries.DownloadFile(documentTemplateId, company);
            stream.Position = 0;
            return File(stream, "application/octet-stream", $"{documentTemplateId}.zip");
        }

        [HttpGet("Events")]
        [ProtectedResource("DocumentTemplate", "view")]
        public async Task<ActionResult<EnumerationDto>> GetAllEvents([FromRoute] Guid company)
        {
            var result = await documentTemplateQueries.GetAllEvents();
            return OkResponse(result);
        }

        [HttpGet("RecoverDataModels")]
        [ProtectedResource("DocumentTemplate", "view")]
        public ActionResult GetAllRecoverDataModels([FromRoute] Guid company)
        {
            var objects = new List<JsonObject>();

            objects.Add(RecoverCompanyInfoToDocumentTemplateService.GetModel());
            objects.Add(RecoverDepartamentInfoToDocumentTemplateService.GetModel());
            objects.Add(RecoverEmployeeInfoToDocumentTemplateService.GetModel());
            objects.Add(RecoverPGRInfoToDocumentTemplateService.GetModel());
            objects.Add(RecoverPositionInfoToDocumentTemplateService.GetModel());
            objects.Add(RecoverRoleInfoToDocumentTemplateService.GetModel());
            objects.Add(RecoverWorkplaceInfoToDocumentTemplateService.GetModel());
            objects.Add(RecoverComplementaryInfoToDocumentTemplateService.GetModel());
     

            return OkResponse(objects);
        }
    }
}
