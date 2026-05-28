namespace EconomicCore.API.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class BaseController(ILogger<BaseController> logger) : ControllerBase
{
    private readonly ILogger<BaseController> _logger = logger;

    protected ActionResult OkResponse(object? result = null)
    {
        return Ok(result);
    }

    protected Guid GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub))
            throw new UnauthorizedAccessException("Token bearer não contém o claim 'sub'.");

        if (!Guid.TryParse(sub, out var userId))
            throw new UnauthorizedAccessException("Claim 'sub' do token não é um GUID válido.");

        return userId;
    }

    protected bool TryGetUserId(out Guid userId)
    {
        try
        {
            userId = GetUserId();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            userId = Guid.Empty;
            return false;
        }
    }

    protected void SendingCommandLog(object? commandId, object? command, Guid requestId)
    {
        var idProperty = commandId?.GetType().Name;
        var commandName = command?.GetType().Name;
        _logger.LogInformation(
            "----- Sending command: {CommandName} - {IdProperty}: {CommandId} ({@Command}) - RequestId : {RequestId} -----",
            commandName,
            idProperty,
            commandId,
            command,
            requestId);
    }

    protected void CommandResultLog(object? result, object? commandId, object? command, Guid requestId)
    {
        var idProperty = commandId?.GetType().Name;
        var commandName = command?.GetType().Name;
        _logger.LogInformation(
            "----- Command result: {@Result} - {CommandName} - {IdProperty}: {CommandId} ({@Command}) - RequestId : {RequestId} -----",
            result,
            commandName,
            idProperty,
            commandId,
            command,
            requestId);
    }
}
