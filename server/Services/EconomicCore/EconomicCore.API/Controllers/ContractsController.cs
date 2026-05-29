namespace EconomicCore.API.Controllers;

using EconomicCore.Application.Commands.ActivateEconomicContract;
using EconomicCore.Application.Commands.GenerateCommitments;
using EconomicCore.Application.Commands.RegisterEconomicContract;
using EconomicCore.Application.Commands.TerminateEconomicContract;
using EconomicCore.Application.Mediator;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/{tenantId:guid}/[controller]")]
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

        var identified = new IdentifiedCommand<RegisterEconomicContractCommand, RegisterEconomicContractResponse>(
            command, EnsureRequestId(requestId));
        var response = await mediator.Send(identified, ct);

        CommandResultLog(response, tenantId, command, requestId);

        return Created($"/api/v1/{tenantId}/contracts/{response.Id}", response);
    }

    [HttpPost("{contractId:guid}/activate")]
    public async Task<ActionResult<ActivateEconomicContractResponse>> Activate(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid contractId,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken ct)
    {
        var command = new ActivateEconomicContractCommand(tenantId, contractId);

        SendingCommandLog(contractId, command, requestId);

        var identified = new IdentifiedCommand<ActivateEconomicContractCommand, ActivateEconomicContractResponse>(
            command, EnsureRequestId(requestId));
        var response = await mediator.Send(identified, ct);

        CommandResultLog(response, contractId, command, requestId);

        return OkResponse(response);
    }

    [HttpPost("{contractId:guid}/terminate")]
    public async Task<ActionResult<TerminateEconomicContractResponse>> Terminate(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid contractId,
        [FromBody] TerminateEconomicContractModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken ct)
    {
        var command = model.ToCommand(tenantId, contractId);

        SendingCommandLog(contractId, command, requestId);

        var identified = new IdentifiedCommand<TerminateEconomicContractCommand, TerminateEconomicContractResponse>(
            command, EnsureRequestId(requestId));
        var response = await mediator.Send(identified, ct);

        CommandResultLog(response, contractId, command, requestId);

        return OkResponse(response);
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

        var identified = new IdentifiedCommand<GenerateCommitmentsCommand, GenerateCommitmentsResponse>(
            command, EnsureRequestId(requestId));
        var response = await mediator.Send(identified, ct);

        CommandResultLog(response, contractId, command, requestId);

        return Created($"/api/v1/{tenantId}/contracts/{contractId}/commitments", response);
    }
}
