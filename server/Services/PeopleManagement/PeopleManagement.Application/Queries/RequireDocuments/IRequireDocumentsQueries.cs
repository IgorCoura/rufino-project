using static PeopleManagement.Application.Queries.RequireDocuments.RequireDocumentsDtos;

namespace PeopleManagement.Application.Queries.RequireDocuments
{
    public interface IRequireDocumentsQueries
    {
        Task<IEnumerable<RequireDocumentSimpleDto>> GetAllSimple(Guid companyId);
        Task<RequireDocumentDto> GetById(Guid requireDocumentId, Guid companyId);
        Task<IEnumerable<AssociationDto>> GetAllAssociationsByType(Guid companyId, int associationTypeId);
        Task<AssociationDto> GetByIdAssociationsByType(Guid companyId, Guid associationId, int associationTypeId);
        Task<IEnumerable<RequiredWithDocumentListDto>> GetAllWithDocumentList(Guid companyId, Guid employee);
    }
}
