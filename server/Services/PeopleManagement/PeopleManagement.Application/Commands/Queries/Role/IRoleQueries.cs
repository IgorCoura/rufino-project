using static PeopleManagement.Application.Commands.Queries.Role.RoleDtos;

namespace PeopleManagement.Application.Commands.Queries.Role
{
    public interface IRoleQueries
    {
        Task<RoleDto> GetRole(Guid id, Guid company);
    }
}
