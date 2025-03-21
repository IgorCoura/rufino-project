using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.API.Controllers
{
    [ApiController]
    public class BaseController(ILogger<BaseController> logger) : ControllerBase
    {
        private readonly ILogger<BaseController> _logger = logger;

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

        protected void SendingCommandLog(object? CommandId, object? Command, Guid RequestId)
        {
            var IdProperty = CommandId?.GetType().Name;
            var CommandName = Command?.GetType().Name;
            _logger.LogInformation(
                    "----- Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command}) - RequestId : {RequestId} -----",
                    CommandName,
                    IdProperty,
                    CommandId,
                    Command,
                    RequestId);
        }

        protected void CommandResultLog(object? result, object? CommandId, object? Command, Guid RequestId)
        {
            var IdProperty = CommandId?.GetType().Name;
            var CommandName = Command?.GetType().Name;
            _logger.LogInformation(
                "----- Command result: {@Result} - {CommandName} - {IdProperty}: {CommandId} ({@Command}) - RequestId : {RequestId} -----",
                result,
                CommandName,
                IdProperty,
                CommandId,
                Command,
                RequestId);
        }

        protected void ValidityRequestId(string requestId)
        {
            if(!Guid.TryParse(requestId, out Guid guid) || guid != Guid.Empty)
                throw new DomainException(this, DomainErrors.FieldInvalid("RequestId", requestId));
        }
    }


}
