namespace EconomicCore.API.Controllers;

using EconomicCore.Application.Queries;
using EconomicCore.Application.Queries.GetCashFlow;
using EconomicCore.Application.Queries.GetCompetenceDRE;
using Microsoft.AspNetCore.Mvc;

[Route("api/v1/{tenantId:guid}/[controller]")]
public sealed class ReportsController(ILogger<ReportsController> logger, IReportQueries reportQueries) : BaseController(logger)
{
    // Query side (CQRS, padrão eShop): leitura chamada direto na interface de queries, sem mediator.
    [HttpGet("dre")]
    public async Task<ActionResult<GetCompetenceDREResponse>> GetDRE(
        [FromRoute] Guid tenantId,
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var response = await reportQueries.GetCompetenceDREAsync(tenantId, year, month, ct);
        return OkResponse(response);
    }

    [HttpGet("cashflow")]
    public async Task<ActionResult<GetCashFlowResponse>> GetCashFlow(
        [FromRoute] Guid tenantId,
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var response = await reportQueries.GetCashFlowAsync(tenantId, year, month, ct);
        return OkResponse(response);
    }
}
