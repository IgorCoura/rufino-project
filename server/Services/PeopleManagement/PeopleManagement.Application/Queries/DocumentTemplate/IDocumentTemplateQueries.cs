using static PeopleManagement.Application.Queries.Base.BaseDtos;
using static PeopleManagement.Application.Queries.DocumentTemplate.DocumentTemplateDtos;

namespace PeopleManagement.Application.Queries.DocumentTemplate
{
    public interface IDocumentTemplateQueries
    {
        Task<IEnumerable<DocumentTemplateDto>> GetAll(Guid companyId);
        Task<IEnumerable<DocumentTemplateSimpleDto>> GetAllSimple(Guid companyId);
        Task<DocumentTemplateDto> GetById(Guid companyId, Guid documentTemplateId);
        Task<bool> HasFile(Guid documentTemplateId, Guid companyId);
        Task<Stream> DownloadFile(Guid documentTemplateId, Guid companyId);
        Task<List<EnumerationDto>> GetAllEvents();
    }
}
