namespace EconomicCore.Application.Queries;

using EconomicCore.Application.Queries.GetCashFlow;
using EconomicCore.Application.Queries.GetCompetenceDRE;
using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class ReportQueries : IReportQueries
{
    private readonly EconomicCoreDbContext _db;

    public ReportQueries(EconomicCoreDbContext db) => _db = db;

    public async Task<GetCompetenceDREResponse> GetCompetenceDREAsync(Guid tenantId, int year, int month, CancellationToken cancellationToken)
    {
        var tenant = TenantId.From(tenantId);

        var totalExpense = await _db.EconomicEvents
            .AsNoTracking()
            .Where(e => e.TenantId.Equals(tenant)
                && e.Direction == FlowDirection.Inflow
                && e.Competence.Year == year
                && e.Competence.Month == month)
            .SumAsync(e => e.Amount.Amount, cancellationToken);

        return new GetCompetenceDREResponse($"{year}-{month:D2}", totalExpense);
    }

    public async Task<GetCashFlowResponse> GetCashFlowAsync(Guid tenantId, int year, int month, CancellationToken cancellationToken)
    {
        var tenant = TenantId.From(tenantId);

        var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfNextMonth = startOfMonth.AddMonths(1);

        var totalOutflow = await _db.EconomicEvents
            .AsNoTracking()
            .Where(e => e.TenantId.Equals(tenant)
                && e.Direction == FlowDirection.Outflow
                && e.OccurredAt.InstantUtc >= startOfMonth
                && e.OccurredAt.InstantUtc < startOfNextMonth)
            .SumAsync(e => e.Amount.Amount, cancellationToken);

        return new GetCashFlowResponse($"{year}-{month:D2}", totalOutflow);
    }
}
