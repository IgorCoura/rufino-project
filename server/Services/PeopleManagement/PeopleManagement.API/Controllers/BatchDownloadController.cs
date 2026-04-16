using PeopleManagement.API.Authorization;
using PeopleManagement.Application.Queries.BatchDownload;
using static PeopleManagement.Application.Queries.BatchDownload.BatchDownloadDtos;

namespace PeopleManagement.API.Controllers
{
    [Route("api/v1/{company}/batch-download")]
    public class BatchDownloadController(
        ILogger<BatchDownloadController> logger,
        IBatchDownloadQueries batchDownloadQueries
    ) : BaseController(logger)
    {
        private readonly IBatchDownloadQueries _batchDownloadQueries = batchDownloadQueries;

        [HttpGet("employees")]
        [ProtectedResource("document", "download")]
        public async Task<ActionResult<BatchDownloadEmployeesResult>> GetEmployees(
            [FromRoute] Guid company,
            [FromQuery] BatchDownloadEmployeeParams filters)
        {
            var result = await _batchDownloadQueries.GetEmployeesForDownload(company, filters);
            return OkResponse(result);
        }

        [HttpPost("units")]
        [ProtectedResource("document", "download")]
        public async Task<ActionResult<BatchDownloadUnitsResult>> GetDocumentUnits(
            [FromRoute] Guid company,
            [FromBody] BatchDownloadUnitParams filters)
        {
            var result = await _batchDownloadQueries.GetDocumentUnitsForDownload(company, filters);
            return OkResponse(result);
        }

        [HttpPost("download")]
        [ProtectedResource("document", "download")]
        public async Task<IActionResult> DownloadBatch(
            [FromRoute] Guid company,
            [FromBody] BatchDownloadRequest request)
        {
            var stream = await _batchDownloadQueries.DownloadBatchDocumentUnits(company, request.Items);
            return File(stream, "application/octet-stream", "documents.zip".ToUpper());
        }
    }
}
