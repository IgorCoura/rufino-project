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
                    join r in _context.Roles on e.RoleId equals r.Id into roleGroup
                    from r in roleGroup.DefaultIfEmpty()
                    select new
                    {
                        Employee = e,
                        Role = r
                    }
                );

            query = query.Where(e => e.Employee.CompanyId == company);


            if (!string.IsNullOrEmpty(pms.Name))
            {
                query = query.Where(e => ((string)e.Employee.Name).Contains(pms.Name.ToUpper()));
            }

            if (!string.IsNullOrEmpty(pms.Role))
            {
                query = query.Where(e => ((string)e.Role.Name).Contains(pms.Role.ToUpper()));
            }

            if (pms.Status.HasValue && Enumeration.TryFromValue<Status>((int)pms.Status) != null)
            {
                query = query.Where(e => e.Employee.Status == (Status)pms.Status);
            }

            query = pms.SortOrder == SortOrder.ASC
                ? query.OrderBy(e => e.Employee.Name)
                : query.OrderByDescending(e => e.Employee.Name);

   
            query = query.Skip(pms.SizeSkip).Take(pms.PageSize);

            var result = await query.Select(o => new EmployeeSimpleDto
            {
                Id = o.Employee.Id,
                Name = o.Employee.Name.Value,
                Registration = o.Employee.Registration == null ? "" : o.Employee.Registration!.Value,
                Status = o.Employee.Status.Id,
                RoleId = o.Employee.RoleId,
                RoleName = o.Role.Name.Value == null ? "" : o.Role.Name.Value,
                CompanyId = o.Employee.CompanyId
            }).ToListAsync();

            return result;
        }




    }
}
