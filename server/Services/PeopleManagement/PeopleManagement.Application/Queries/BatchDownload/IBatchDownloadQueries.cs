using static PeopleManagement.Application.Queries.BatchDownload.BatchDownloadDtos;

namespace PeopleManagement.Application.Queries.BatchDownload
{
    public interface IBatchDownloadQueries
    {
        Task<BatchDownloadEmployeesResult> GetEmployeesForDownload(Guid companyId, BatchDownloadEmployeeParams filters);
        Task<BatchDownloadUnitsResult> GetDocumentUnitsForDownload(Guid companyId, BatchDownloadUnitParams filters);
        Task<Stream> DownloadBatchDocumentUnits(Guid companyId, IEnumerable<BatchDownloadItem> items);
    }
}
