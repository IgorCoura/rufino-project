using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Commands.ArchiveCommands.InsertFile;
using PeopleManagement.Application.Commands.ArchiveCommands.NotApplicable;
using PeopleManagement.Application.Commands.Identified;
namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/[controller]")]
    public class ArchiveController(ILogger<CompanyController> logger, IMediator mediator) : BaseController(logger)
    {

        [HttpPost("file")]
        [ProtectedResource("Archive", "edit")]
        public async Task<ActionResult<InsertFileResponse>> InsertFile(IFormFile formFile, [FromRoute] Guid company, [FromForm] InsertFileModel request,[FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var extension = Path.GetExtension(formFile.FileName);
            var stream = formFile.OpenReadStream();

            var command = new IdentifiedCommand<InsertFileCommand, InsertFileResponse>(request.ToCommand(company, extension, stream), requestId);

            SendingCommandLog(request.OwnerId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.OwnerId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("file/notapplicable")]
        [ProtectedResource("Archive", "edit")]
        public async Task<ActionResult<FileNotApplicableResponse>> FileNotApplicable([FromRoute] Guid company, [FromBody] FileNotApplicableModel request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<FileNotApplicableCommand, FileNotApplicableResponse>(request.ToCommand(company), requestId);

            SendingCommandLog(request.FileName, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.FileName, request, requestId);

            return OkResponse(result);
        }

    }
}
