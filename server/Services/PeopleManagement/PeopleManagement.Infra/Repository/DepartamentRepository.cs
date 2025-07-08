using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate.Interfaces;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class DepartamentRepository(PeopleManagementContext context) : Repository<Department>(context), IDepartmentRepository
    {
        public async Task<Department?> GetDepartmentFromEmployeeId(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var query = from e in _context.Employees
                        join r in _context.Roles on e.RoleId equals r.Id
                        join p in _context.Positions on r.PositionId equals p.Id
                        join d in _context.Departments on p.DepartmentId equals d.Id
                        where e.Id == employeeId && e.CompanyId == companyId
                        select d;

            return await query.SingleOrDefaultAsync();
        }
    }
}
