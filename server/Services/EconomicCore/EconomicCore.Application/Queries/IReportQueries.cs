namespace EconomicCore.Application.Queries;

using EconomicCore.Application.Queries.GetCashFlow;
using EconomicCore.Application.Queries.GetCompetenceDRE;

/// <summary>
/// Query side dos relatórios (CQRS, padrão eShop IOrderQueries): chamado direto pelo controller,
/// sem mediator. A implementação lê o banco via DbContext com AsNoTracking — exceção autorizada
/// de referência Application → Infra, exclusiva do query side.
/// </summary>
public interface IReportQueries
{
    Task<GetCompetenceDREResponse> GetCompetenceDREAsync(Guid tenantId, int year, int month, CancellationToken cancellationToken);

    Task<GetCashFlowResponse> GetCashFlowAsync(Guid tenantId, int year, int month, CancellationToken cancellationToken);
}
