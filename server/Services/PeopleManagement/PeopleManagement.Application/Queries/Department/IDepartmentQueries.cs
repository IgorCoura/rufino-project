using static PeopleManagement.Application.Queries.Department.DepartmentDtos;

namespace PeopleManagement.Application.Queries.Department
{
    public interface IDepartmentQueries
    {
        Task<IEnumerable<DepartmentDto>> GetAll(Guid company);
    }
}
