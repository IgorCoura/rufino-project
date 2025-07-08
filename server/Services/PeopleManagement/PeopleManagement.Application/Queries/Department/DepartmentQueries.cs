using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.ArchiveCategoryCommands.CreateArchiveCategory;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Queries.Department.DepartmentDtos;

namespace PeopleManagement.Application.Queries.Department
{
    public class DepartmentQueries(PeopleManagementContext peopleManagementContext) : IDepartmentQueries
    {
        private PeopleManagementContext _context = peopleManagementContext;

        public async Task<IEnumerable<DepartmentDto>> GetAll(Guid company)
        {
            var query = from d in _context.Departments.AsNoTracking()
                        where d.CompanyId == company
                        select new DepartmentDto
                        {
                            Id = d.Id,
                            Name = d.Name.Value,
                            Description = d.Description.Value,
                            CompanyId = d.CompanyId,
                            Positions = (from p in _context.Positions
                                         where p.DepartmentId == d.Id
                                         select new DepartmentPositionDto
                                         {
                                             Id = p.Id,
                                             Name = p.Name.Value,
                                             Description = p.Description.Value,
                                             Roles = (from r in _context.Roles
                                                      where r.PositionId == p.Id
                                                      select new DepartmentRoleDto
                                                      {
                                                          Id = r.Id,
                                                          Name = r.Name.Value,
                                                          Description = r.Description.Value
                                                      }).ToList()
                                         }).ToList()
                        };


            var result = await query.ToArrayAsync();
            return result;
        }



        public async Task<IEnumerable<DepartmentSimpleDto>> GetAllSimple(Guid company)
        {
            var query = from d in _context.Departments.AsNoTracking()
                        where d.CompanyId == company
                        select new DepartmentSimpleDto
                        {
                            Id = d.Id,
                            Name = d.Name.Value,
                            Description = d.Description.Value,
                            CompanyId = d.CompanyId,
                        };


            var result = await query.ToArrayAsync();
            return result;
        }
        public async Task<DepartmentSimpleDto> GetById(Guid departmentId, Guid companyId)
        {
            var query = from d in _context.Departments.AsNoTracking()
                        where d.Id == departmentId && d.CompanyId == companyId
                        select new DepartmentSimpleDto
                        {
                            Id = d.Id,
                            Name = d.Name.Value,
                            Description = d.Description.Value,
                            CompanyId = d.CompanyId,
                        };

            var result = await query.FirstOrDefaultAsync() 
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Department), departmentId.ToString()));
            return result;
        }
       
    }
}
