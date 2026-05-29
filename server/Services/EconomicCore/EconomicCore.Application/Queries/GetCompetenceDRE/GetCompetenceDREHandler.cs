namespace EconomicCore.Application.Queries.GetCompetenceDRE;

using EconomicCore.Domain.Operational.EconomicEvents.Enumerations;
using EconomicCore.Domain.SharedKernel;
using EconomicCore.Infra.Persistence;
using EconomicCore.Application.Mediator;
using Microsoft.EntityFrameworkCore;

internal sealed class GetCompetenceDREHandler : IRequestHandler<GetCompetenceDREQuery, GetCompetenceDREResponse>
{
    private readonly EconomicCoreDbContext _db;

    public GetCompetenceDREHandler(EconomicCoreDbContext db) => _db = db;

    public async Task<GetCompetenceDREResponse> Handle(GetCompetenceDREQuery request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var totalExpense = await _db.EconomicEvents
            .AsNoTracking()
            .Where(e => e.TenantId.Equals(tenantId)
                && e.Direction == FlowDirection.Inflow
                && e.Competence.Year == request.Year
                && e.Competence.Month == request.Month)
            .SumAsync(e => e.Amount.Amount, cancellationToken);

        return new GetCompetenceDREResponse(
            $"{request.Year}-{request.Month:D2}",
            totalExpense);
    }
}
