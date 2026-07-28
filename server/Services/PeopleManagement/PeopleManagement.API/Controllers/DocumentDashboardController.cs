using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Queries.DocumentDashboard;
using static PeopleManagement.Application.Queries.DocumentDashboard.DocumentDashboardDtos;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/document-dashboard")]
    public class DocumentDashboardController(
        ILogger<DocumentDashboardController> logger,
        IDocumentDashboardQueries documentDashboardQueries
    ) : BaseController(logger)
    {
        private readonly IDocumentDashboardQueries _documentDashboardQueries = documentDashboardQueries;

        [HttpGet("summary")]
        [ProtectedResource("document", "view")]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary(
            [FromRoute] Guid company,
            [FromQuery] DashboardFilterParams filters)
        {
            var result = await _documentDashboardQueries.GetSummary(company, filters);
            return OkResponse(result);
        }

        [HttpGet("units")]
        [ProtectedResource("document", "view")]
        public async Task<ActionResult<DashboardUnitsResult>> GetUnits(
            [FromRoute] Guid company,
            [FromQuery] DashboardUnitsParams filters)
        {
            var result = await _documentDashboardQueries.GetUnits(company, filters);
            return OkResponse(result);
        }
    }
}
