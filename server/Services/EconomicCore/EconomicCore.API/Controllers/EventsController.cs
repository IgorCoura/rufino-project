namespace EconomicCore.API.Controllers;

using EconomicCore.Application.Commands.RegisterConsumptionEvent;
using EconomicCore.Application.Commands.RegisterPaymentEvent;
using EconomicCore.Application.Mediator;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/{tenantId:guid}/[controller]")]
public sealed class EventsController(ILogger<EventsController> logger, IMediator mediator) : BaseController(logger)
{
    [HttpPost("consumption")]
    public async Task<ActionResult<RegisterConsumptionEventResponse>> RegisterConsumption(
        [FromRoute] Guid tenantId,
        [FromBody] RegisterConsumptionEventModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken ct)
    {
        Guid? userId = TryGetUserId(out var uid) ? uid : null;
        var command = model.ToCommand(tenantId, userId);

        SendingCommandLog(model.ContractId, command, requestId);

        var identified = new IdentifiedCommand<RegisterConsumptionEventCommand, RegisterConsumptionEventResponse>(
            command, EnsureRequestId(requestId));
        var response = await mediator.Send(identified, ct);

        CommandResultLog(response, model.ContractId, command, requestId);

        return Created($"/api/v1/{tenantId}/events/{response.Id}", response);
    }

    [HttpPost("payment")]
    public async Task<ActionResult<RegisterPaymentEventResponse>> RegisterPayment(
        [FromRoute] Guid tenantId,
        [FromBody] RegisterPaymentEventModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken ct)
    {
        Guid? userId = TryGetUserId(out var uid) ? uid : null;
        var command = model.ToCommand(tenantId, userId);

        SendingCommandLog(model.ContractId, command, requestId);

        var identified = new IdentifiedCommand<RegisterPaymentEventCommand, RegisterPaymentEventResponse>(
            command, EnsureRequestId(requestId));
        var response = await mediator.Send(identified, ct);

        CommandResultLog(response, model.ContractId, command, requestId);

        return Created($"/api/v1/{tenantId}/events/{response.Id}", response);
    }
}
