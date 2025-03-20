using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Commands.Queries.Role.RoleDtos;
using static PeopleManagement.Application.Commands.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Commands.Queries.Role
{
    public class RoleQueries(PeopleManagementContext peopleManagementContext) : IRoleQueries
    {
        private PeopleManagementContext _context = peopleManagementContext;

        public async Task<RoleDto> GetRole(Guid id, Guid company)
        {
            var query = from r in _context.Roles
                        join p in _context.Positions on r.PositionId equals p.Id
                        join d in _context.Departments on p.DepartmentId equals d.Id
                        where r.Id == id && r.CompanyId == company
                        select new 
                        { 
                          Role = r, 
                          Position = p, 
                          Departments = d 
                        };

            var result = await query.Select(o => new RoleDto
            {
                Id = o.Role.Id,
                Name = o.Role.Name,
                Description = o.Role.Description.Value,
                CBO = o.Role.CBO.Value,
                Remuneration = new RemunerationDto
                {
                    PaymentUnit = new EnumerationDto
                    {
                        Id = o.Role.Remuneration.PaymentUnit.Id,
                        Name = o.Role.Remuneration.PaymentUnit.Name
                    },
                    BaseSalary = new CurrencyDto
                    {
                        Type = new EnumerationDto
                        {
                            Id = o.Role.Remuneration.BaseSalary.Type.Id,
                            Name = o.Role.Remuneration.BaseSalary.Type.Name
                        },
                        Value = o.Role.Remuneration.BaseSalary.Value
                    },
                    Description = o.Role.Remuneration.Description.Value
                },
                PositionId = o.Role.PositionId,
                Position = new PositionDto
                {
                    Name = o.Position.Name.Value,
                    Description = o.Position.Description.Value,
                    CBO = o.Position.CBO.Value,
                    DepartmentId = o.Position.DepartmentId,
                    Department = new DepartmentDto
                    {
                        Name = o.Departments.Name.Value,
                        Description = o.Departments.Description.Value
                    }

                }
            }).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));

            return result;
        }

    }
}
