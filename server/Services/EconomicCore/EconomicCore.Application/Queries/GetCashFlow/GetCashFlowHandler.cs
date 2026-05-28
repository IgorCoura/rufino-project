namespace EconomicCore.Application.Queries.GetCashFlow;

using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.Infra.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

internal sealed class GetCashFlowHandler : IRequestHandler<GetCashFlowQuery, GetCashFlowResponse>
{
    private readonly EconomicCoreDbContext _db;

    public GetCashFlowHandler(EconomicCoreDbContext db) => _db = db;

    public async Task<GetCashFlowResponse> Handle(GetCashFlowQuery request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var startOfMonth = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfNextMonth = startOfMonth.AddMonths(1);

        var totalOutflow = await _db.EconomicEvents
            .AsNoTracking()
            .Where(e => e.TenantId.Equals(tenantId)
                && e.Direction == FlowDirection.Outflow
                && e.OccurredAt.InstantUtc >= startOfMonth
                && e.OccurredAt.InstantUtc < startOfNextMonth)
            .SumAsync(e => e.Amount.Amount, cancellationToken);

        return new GetCashFlowResponse(
            $"{request.Year}-{request.Month:D2}",
            totalOutflow);
    }
}
