using PeopleManagement.Application.Commands.CreateCompany;
using PeopleManagement.Application.Commands.DTO;
using PeopleManagement.Application.Commands.Identified;
using System.ComponentModel.Design;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(IMediator mediator, ILogger<CompanyController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<BaseDTO>> CreateCompany([FromBody] CreateCompanyCommand request, [FromHeader(Name = "x-requestid")] string requestId)
        {
           
            if (Guid.TryParse(requestId, out Guid guid) && guid != Guid.Empty)
            {
                var command = new IdentifiedCommand<CreateCompanyCommand, BaseDTO>(request, guid);

                _logger.LogInformation(
                    "----- Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
                    request.GetType().Name,
                    nameof(request.Cnpj),
                    request.Cnpj,
                    request);

                var result = await _mediator.Send(command);

                _logger.LogInformation(
                "----- Command result: {@Result} - {CommandName} - {IdProperty}: {CommandId} ({@Command})",
                result,
                request.GetType().Name,
                nameof(request.Cnpj),
                request.Cnpj,
                command);

                return Created("", result);
            }
            else
            {
                return BadRequest("Invalid request Id");
            }

            
        }
    }
}
