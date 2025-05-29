using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class PositionRepository(PeopleManagementContext context) : Repository<Position>(context), IPositionRepository
    {
        public async Task<Position?> GetPositionFromEmployeeId(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var query = from e in _context.Employees
                        join r in _context.Roles on e.RoleId equals r.Id
                        join p in _context.Positions on r.PositionId equals p.Id
                        where e.Id == employeeId && e.CompanyId == companyId
                        select p;

            return await query.SingleOrDefaultAsync();
        }
    }
}
