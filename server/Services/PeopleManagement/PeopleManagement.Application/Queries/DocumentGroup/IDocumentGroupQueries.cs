using static PeopleManagement.Application.Queries.DocumentGroup.DocumentGroupDtos;

namespace PeopleManagement.Application.Queries.DocumentGroup
{
    public interface IDocumentGroupQueries
    {
        Task<IEnumerable<DocumentGroupDto>> GetAll(Guid company);
        Task<IEnumerable<DocumentGroupWithDocumentsDto>> GetAllWithDocuments(Guid companyId, Guid employeeId);
    }
}
