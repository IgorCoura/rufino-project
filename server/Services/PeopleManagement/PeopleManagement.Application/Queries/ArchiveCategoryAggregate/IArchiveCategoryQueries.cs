using static PeopleManagement.Application.Queries.ArchiveCategoryAggregate.ArchiveCategoryDtos;

namespace PeopleManagement.Application.Queries.ArchiveCategoryAggregate
{
    public interface IArchiveCategoryQueries
    {
        Task<IEnumerable<ArchiveCategoryDTO>> GetAll(Guid companyId);
    }
}
