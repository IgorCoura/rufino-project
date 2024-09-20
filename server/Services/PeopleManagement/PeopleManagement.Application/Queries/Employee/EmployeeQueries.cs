using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.SeedWord;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Application.Queries.Employee
{
    public class EmployeeQueries(PeopleManagementContext peopleManagementContext) : IEmployeeQueries
    {
        private PeopleManagementContext _context = peopleManagementContext;

        public async Task<IEnumerable<EmployeeSimpleDto>> GetEmployeeList(EmployeeParams pms, Guid company)
        {
            var query = 
                (
                    from e in _context.Employees
                    join r in _context.Roles on e.RoleId equals r.Id
                    select new
                    {
                        Employee = e,
                        Role = r
                    }
                );


            if (!string.IsNullOrEmpty(pms.Name))
            {
                query = query.Where(e => e.Employee.Name.Value.Contains((string)pms.Name));
            }

            if (pms.Status.HasValue && Enumeration.TryFromValue<Status>((int)pms.Status) != null)
            {
                query = query.Where(e => e.Employee.Status == (Status)pms.Status);
            }

            query = pms.SortOrder == SortOrder.ASC
                ? query.OrderBy(e => e.Employee.Name)
                : query.OrderByDescending(e => e.Employee.Name);

            var pageNumber = pms.PageNumber <= 0 ? 1 : pms.PageNumber;
            query = query.Skip((pageNumber - 1) * pms.PageSize).Take(pms.PageSize);

            var result = await query.Select(o => new EmployeeSimpleDto
            {
                Id = o.Employee.Id,
                Name = o.Employee.Name.Value,
                Registration = o.Employee.Registration == null ? "" : o.Employee.Registration!.Value,
                Status = o.Employee.Status.Id,
                RoleId = o.Employee.RoleId,
                RoleName = o.Role.Name.Value,
            }).ToListAsync();

            return result;
        }




    }
}
