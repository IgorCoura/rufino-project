using static PeopleManagement.Application.Queries.Document.DocumentDtos;

namespace PeopleManagement.Application.Queries.Document
{
    public interface IDocumentQueries
    {
        Task<IEnumerable<DocumentSimpleDto>> GetAllSimple(Guid employeeId, Guid companyId);
        Task<DocumentDto> GetById(Guid documentId, Guid employeeId, Guid companyId);
        Task<Stream> DownloadDocumentUnit(Guid documentUnitId, Guid documentId, Guid employeeId, Guid companyId);
    }
}
