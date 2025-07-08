using static PeopleManagement.Application.Queries.Workplace.WorkplaceDtos;

namespace PeopleManagement.Application.Queries.Workplace
{
    public interface IWorkplaceQueries
    {
        Task<WorkplaceDto> GetByIdAsync(Guid workplaceId, Guid companyId);
        Task<IEnumerable<WorkplaceDto>> GetAllAsync(Guid companyId);
    }
}
