namespace EconomicCore.API.Controllers;

using EconomicCore.Application.Commands.RegisterEconomicAgent;
using EconomicCore.Application.Mediator;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/{tenantId:guid}/[controller]")]
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

        var identified = new IdentifiedCommand<RegisterEconomicAgentCommand, RegisterEconomicAgentResponse>(
            command, EnsureRequestId(requestId));
        var response = await mediator.Send(identified, ct);

        CommandResultLog(response, tenantId, command, requestId);

        return Created($"/api/v1/{tenantId}/agents/{response.Id}", response);
    }
}
