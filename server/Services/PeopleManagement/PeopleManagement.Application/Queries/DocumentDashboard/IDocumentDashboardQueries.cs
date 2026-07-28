using static PeopleManagement.Application.Queries.DocumentDashboard.DocumentDashboardDtos;

namespace PeopleManagement.Application.Queries.DocumentDashboard
{
    public interface IDocumentDashboardQueries
    {
        /// <summary>
        /// Conta as unidades de documento da empresa em cada bucket operacional
        /// (vencidos, a vencer, pendentes, aguardando assinatura, requer validação),
        /// aplicando os mesmos filtros usados pela listagem.
        /// </summary>
        Task<DashboardSummaryDto> GetSummary(Guid companyId, DashboardFilterParams filters);

        /// <summary>
        /// Lista paginada das unidades de documento da empresa dentro do bucket
        /// informado, ordenada por urgência (validade mais próxima primeiro).
        /// </summary>
        Task<DashboardUnitsResult> GetUnits(Guid companyId, DashboardUnitsParams filters);
    }
}
