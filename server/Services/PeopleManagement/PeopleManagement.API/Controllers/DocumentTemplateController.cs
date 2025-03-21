using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.CreateDocumentTemplate;
using PeopleManagement.Application.Commands.DocumentTemplateCommands.InsertDocumentTemplate;
using PeopleManagement.Application.Commands.Identified;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class DocumentTemplateController(ILogger<DocumentTemplateController> logger, IMediator mediator) : BaseController(logger)
    {
        [HttpPost("Create")]
        [ProtectedResource("DocumentTemplate", "create")]
        public async Task<ActionResult<CreateDocumentTemplateResponse>> Create([FromBody] CreateDocumentTemplateCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<CreateDocumentTemplateCommand, CreateDocumentTemplateResponse>(request, requestId);

            SendingCommandLog(request.CompanyId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.CompanyId, request, requestId);

            return OkResponse(result);
        }

        [HttpPost("Insert")]
        [ProtectedResource("DocumentTemplate", "send")]
        public async Task<ActionResult<InsertDocumentTemplateResponse>> Insert(IFormFile formFile, [FromForm] Guid id, [FromForm] Guid companyId, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var stream = formFile.OpenReadStream();
            var request = new InsertDocumentTemplateCommand(id, companyId, formFile.FileName, stream);
            var command = new IdentifiedCommand<InsertDocumentTemplateCommand, InsertDocumentTemplateResponse>(request, requestId);

            SendingCommandLog(request.DocumentTemplateId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.DocumentTemplateId, request, requestId);

            return OkResponse(result);
        }

    }
}
