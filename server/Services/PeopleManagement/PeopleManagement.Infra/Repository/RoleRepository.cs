using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class RoleRepository(PeopleManagementContext context) : Repository<Role>(context), IRoleRepository
    {
        public async Task<Role?> GetRoleFromEmployeeId(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var query = from e in _context.Employees
                        join r in _context.Roles on e.RoleId equals r.Id
                        where e.Id == employeeId && e.CompanyId == companyId
                        select r;

            return await query.SingleOrDefaultAsync();
        }
    }
}
