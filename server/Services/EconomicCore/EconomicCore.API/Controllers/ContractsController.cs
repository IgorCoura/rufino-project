namespace EconomicCore.API.Controllers;

using EconomicCore.Application.Commands.GenerateCommitments;
using EconomicCore.Application.Commands.RegisterEconomicContract;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/{tenantId}/[controller]")]
public sealed class ContractsController(ILogger<ContractsController> logger, IMediator mediator) : BaseController(logger)
{
    [HttpPost]
    public async Task<ActionResult<RegisterEconomicContractResponse>> Create(
        [FromRoute] Guid tenantId,
        [FromBody] RegisterEconomicContractModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken ct)
    {
        var command = model.ToCommand(tenantId);

        SendingCommandLog(tenantId, command, requestId);

        var response = await mediator.Send(command, ct);

        CommandResultLog(response, tenantId, command, requestId);

        return Created($"/api/v1/{tenantId}/contracts/{response.Id}", response);
    }

    [HttpPost("{contractId:guid}/commitments")]
    public async Task<ActionResult<GenerateCommitmentsResponse>> GenerateCommitments(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid contractId,
        [FromBody] GenerateCommitmentsModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken ct)
    {
        var command = model.ToCommand(tenantId, contractId);

        SendingCommandLog(contractId, command, requestId);

        var response = await mediator.Send(command, ct);

        CommandResultLog(response, contractId, command, requestId);

        return Created($"/api/v1/{tenantId}/contracts/{contractId}/commitments", response);
    }
}
