namespace EconomicCore.API.Controllers;

using EconomicCore.Application.Commands.RegisterEconomicAgent;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/{tenantId}/[controller]")]
public sealed class AgentsController(ILogger<AgentsController> logger, IMediator mediator) : BaseController(logger)
{
    [HttpPost]
    public async Task<ActionResult<RegisterEconomicAgentResponse>> Create(
        [FromRoute] Guid tenantId,
        [FromBody] RegisterEconomicAgentModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken ct)
    {
        var command = model.ToCommand(tenantId);

        SendingCommandLog(tenantId, command, requestId);

        var response = await mediator.Send(command, ct);

        CommandResultLog(response, tenantId, command, requestId);

        return Created($"/api/v1/{tenantId}/agents/{response.Id}", response);
    }
}
