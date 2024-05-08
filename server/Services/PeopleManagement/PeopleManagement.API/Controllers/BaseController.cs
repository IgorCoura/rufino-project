using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.API.Controllers
{
    [ApiController]
    public class BaseController : ControllerBase
    {
        private readonly ILogger<CompanyController> _logger;

        public BaseController(ILogger<CompanyController> logger)
        {
            _logger = logger;
        }

        protected ActionResult OkResponse(object? result = null)
        {
            return Ok(result);
        }

        protected ActionResult BadRequestResponse(object? result = null)
        {
            return BadRequestResponse(new
            {
                data = result
            });
        }

        protected void SendingCommandLog(object? CommandName, object? IdProperty, object? CommandId, object? Command)
        {
            _logger.LogInformation(
                    "----- Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command})",
                    CommandName,
                    IdProperty,
                    CommandId,
                    Command);
        }

        protected void CommandResultLog(object? result, object? CommandName, object? IdProperty, object? CommandId, object? Command)
        {
            _logger.LogInformation(
                "----- Command result: {@Result} - {CommandName} - {IdProperty}: {CommandId} ({@Command})",
                result,
                CommandName,
                IdProperty,
                CommandId,
                Command);
        }

        protected void ValidityRequestId(string requestId)
        {
            if(!Guid.TryParse(requestId, out Guid guid) || guid != Guid.Empty)
                throw new DomainException(this, DomainErrors.FieldInvalid("RequestId", requestId));
        }
    }


}
