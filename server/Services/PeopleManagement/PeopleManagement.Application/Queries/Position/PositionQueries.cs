using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Queries.Position.PositionDtos;

namespace PeopleManagement.Application.Queries.Position
{
    public class PositionQueries(PeopleManagementContext peopleManagementContext) : IPositionQueries
    {
        private PeopleManagementContext _context = peopleManagementContext;
        public async Task<IEnumerable<PositionSimpleDto>> GetAllSimple(Guid departmentId, Guid company)
        {
            var query = from d in _context.Positions
                        where d.CompanyId == company && d.DepartmentId == departmentId
                        select new PositionSimpleDto
                        {
                            Id = d.Id,
                            Name = d.Name.Value,
                            Description = d.Description.Value,
                            CBO = d.CBO.Value,
                        };


            var result = await query.ToArrayAsync();
            return result;
        }
        public async Task<PositionSimpleDto> GetById(Guid positionId, Guid company)
        {
            var query = from d in _context.Positions
                        where d.Id == positionId && d.CompanyId == company
                        select new PositionSimpleDto
                        {
                            Id = d.Id,
                            Name = d.Name.Value,
                            Description = d.Description.Value,
                            CBO = d.CBO.Value,
                        };

            var result = await query.FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Position), positionId.ToString()));
            return result;
        }
        

    }
}
