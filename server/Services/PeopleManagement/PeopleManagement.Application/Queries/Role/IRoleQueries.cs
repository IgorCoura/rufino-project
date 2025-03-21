using static PeopleManagement.Application.Queries.Role.RoleDtos;

namespace PeopleManagement.Application.Queries.Role
{
    public interface IRoleQueries
    {
        Task<RoleDto> GetRole(Guid id, Guid company);
        Task<IEnumerable<RoleSimpleDto>> GetAllSimpleRoles(Guid PositionId, Guid company);
    }
}
