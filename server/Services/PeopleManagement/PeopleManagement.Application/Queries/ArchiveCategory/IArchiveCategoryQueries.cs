using static PeopleManagement.Application.Queries.ArchiveCategory.ArchiveCategoryDtos;

namespace PeopleManagement.Application.Queries.ArchiveCategory
{
    public interface IArchiveCategoryQueries
    {
        Task<IEnumerable<ArchiveCategoryDTO>> GetAll(Guid companyId);
    }
}
