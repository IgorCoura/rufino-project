
using PeopleManagement.Application.Commands.EmployeeCommands.AlterMilitarDocumentEmployee;
using PeopleManagement.Application.Commands.Identified;
using System.Net;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    public class SecurityDocumentController : BaseController
    {
        
        public SecurityDocumentController(ILogger<CompanyController> logger) : base(logger)
        {
        }

        [HttpGet("pdf")]
        public Task<FileContentResult> GeneratePdf([FromHeader(Name = "x-requestid")] Guid requestId)
        {
            //var command = new IdentifiedCommand<AlterMilitarDocumentEmployeeCommand, AlterMilitarDocumentEmployeeResponse>(request, requestId);

            //SendingCommandLog(request.EmployeeId, request, requestId);

            //var result = await _mediator.Send(command);

            //CommandResultLog(result, request.EmployeeId, request, requestId);

            //return OkResponse(result);
            var pdfBytes = new byte[100];

            var id = Guid.NewGuid();

            var response = new FileContentResult(pdfBytes, "application/pdf")
            {
                FileDownloadName = $"{id}.pdf"
            };

            return Task.FromResult(response);

        }

    }
}
