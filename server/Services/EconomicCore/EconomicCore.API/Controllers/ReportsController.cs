namespace EconomicCore.API.Controllers;

using EconomicCore.Application.Mediator;
using EconomicCore.Application.Queries.GetCashFlow;
using EconomicCore.Application.Queries.GetCompetenceDRE;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/{tenantId:guid}/[controller]")]
public sealed class ReportsController(ILogger<ReportsController> logger, IMediator mediator) : BaseController(logger)
{
    [HttpGet("dre")]
    public async Task<ActionResult<GetCompetenceDREResponse>> GetDRE(
        [FromRoute] Guid tenantId,
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var query = new GetCompetenceDREQuery(tenantId, year, month);
        var response = await mediator.Send(query, ct);
        return OkResponse(response);
    }

    [HttpGet("cashflow")]
    public async Task<ActionResult<GetCashFlowResponse>> GetCashFlow(
        [FromRoute] Guid tenantId,
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var query = new GetCashFlowQuery(tenantId, year, month);
        var response = await mediator.Send(query, ct);
        return OkResponse(response);
    }
}
