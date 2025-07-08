using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Queries.Role.RoleDtos;
using static PeopleManagement.Application.Queries.Base.BaseDtos;

namespace PeopleManagement.Application.Queries.Role
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
                Remuneration = new RoleRemunerationDto
                {
                    PaymentUnit = new EnumerationDto
                    {
                        Id = o.Role.Remuneration.PaymentUnit.Id,
                        Name = o.Role.Remuneration.PaymentUnit.Name
                    },
                    BaseSalary = new RoleCurrencyDto
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
                Position = new RolePositionDto
                {
                    Id = o.Position.Id,
                    Name = o.Position.Name.Value,
                    Description = o.Position.Description.Value,
                    CBO = o.Position.CBO.Value,
                    DepartmentId = o.Position.DepartmentId,
                    Department = new RoleDepartmentDto
                    {
                        Id = o.Departments.Id,
                        Name = o.Departments.Name.Value,
                        Description = o.Departments.Description.Value
                    }

                }
            }).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), id.ToString()));

            return result;
        }

        public async Task<IEnumerable<RoleSimpleDto>> GetAllSimpleRoles(Guid PositionId, Guid company)
        {
            var query = _context.Roles.Where(x => x.PositionId == PositionId && x.CompanyId == company);

            var result = await query.Select(x => new RoleSimpleDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description.Value,
                CBO = x.CBO.Value,
                Remuneration = new RoleRemunerationDto
                {
                    PaymentUnit = new EnumerationDto
                    {
                        Id = x.Remuneration.PaymentUnit.Id,
                        Name = x.Remuneration.PaymentUnit.Name
                    },
                    BaseSalary = new RoleCurrencyDto
                    {
                        Type = new EnumerationDto
                        {
                            Id = x.Remuneration.BaseSalary.Type.Id,
                            Name = x.Remuneration.BaseSalary.Type.Name
                        },
                        Value = x.Remuneration.BaseSalary.Value
                    },
                    Description = x.Remuneration.Description.Value
                },
                PositionId = x.PositionId,
                CompanyId = x.CompanyId
            }).ToArrayAsync();

            return result;
        }


    }
}
