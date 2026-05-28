namespace EconomicCore.API.Controllers;

using EconomicCore.Application.Commands.RegisterEconomicResource;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/{tenantId}/[controller]")]
public sealed class ResourcesController(ILogger<ResourcesController> logger, IMediator mediator) : BaseController(logger)
{
    [HttpPost]
    public async Task<ActionResult<RegisterEconomicResourceResponse>> Create(
        [FromRoute] Guid tenantId,
        [FromBody] RegisterEconomicResourceModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken ct)
    {
        var command = model.ToCommand(tenantId);

        SendingCommandLog(tenantId, command, requestId);

        var response = await mediator.Send(command, ct);

        CommandResultLog(response, tenantId, command, requestId);

        return Created($"/api/v1/{tenantId}/resources/{response.Id}", response);
    }
}
