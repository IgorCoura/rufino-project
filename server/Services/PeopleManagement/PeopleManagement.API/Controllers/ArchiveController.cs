using PeopleManagement.Application.Commands.ArchiveCommands.InsertFile;
using PeopleManagement.Application.Commands.ArchiveCommands.NotApplicable;
using PeopleManagement.Application.Commands.Identified;
namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class ArchiveController(ILogger<CompanyController> logger, IMediator mediator) : BaseController(logger)
    {

        [HttpPost("file/insert")]
        public async Task<ActionResult<InsertFileResponse>> InsertFile(IFormFile formFile, [FromForm] Guid ownerId, [FromForm] Guid companyId, [FromForm] Guid categoryId, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var extension = Path.GetExtension(formFile.FileName);
            var stream = formFile.OpenReadStream();

            var request = new InsertFileCommand(ownerId, companyId, categoryId, extension, stream);
            var command = new IdentifiedCommand<InsertFileCommand, InsertFileResponse>(request, requestId);

            SendingCommandLog(request.OwnerId, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.OwnerId, request, requestId);

            return OkResponse(result);
        }

        [HttpPut("file/notapplicable")]
        public async Task<ActionResult<FileNotApplicableResponse>> FileNotApplicable([FromBody] FileNotApplicableCommand request, [FromHeader(Name = "x-requestid")] Guid requestId)
        {
            var command = new IdentifiedCommand<FileNotApplicableCommand, FileNotApplicableResponse>(request, requestId);

            SendingCommandLog(request.FileName, request, requestId);

            var result = await mediator.Send(command);

            CommandResultLog(result, request.FileName, request, requestId);

            return OkResponse(result);
        }

    }
}
