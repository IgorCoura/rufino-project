using static PeopleManagement.Application.Queries.Position.PositionDtos;

namespace PeopleManagement.Application.Queries.Position
{
    public interface IPositionQueries
    {
        Task<IEnumerable<PositionSimpleDto>> GetAllSimple(Guid departmentId, Guid company);
        Task<PositionSimpleDto> GetById(Guid positionId, Guid company);
    }
}
