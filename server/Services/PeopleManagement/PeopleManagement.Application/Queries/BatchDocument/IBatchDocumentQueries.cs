using static PeopleManagement.Application.Queries.BatchDocument.BatchDocumentDtos;

namespace PeopleManagement.Application.Queries.BatchDocument
{
    public interface IBatchDocumentQueries
    {
        Task<BatchDocumentUnitsResult> GetPendingDocumentUnits(Guid companyId, Guid documentTemplateId, BatchDocumentUnitParams filters);
        Task<IEnumerable<EmployeeMissingDocumentDto>> GetEmployeesWithoutPendingDocument(Guid companyId, Guid documentTemplateId, BatchDocumentUnitParams filters);
    }
}
