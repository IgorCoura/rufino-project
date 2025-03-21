using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Infra.Context;
using static PeopleManagement.Application.Queries.Workplace.WorkplaceDtos;

namespace PeopleManagement.Application.Queries.Workplace
{
    public class WorkplaceQueries(PeopleManagementContext peopleManagementContext) : IWorkplaceQueries
    {
        private PeopleManagementContext _context = peopleManagementContext;

        public async Task<WorkplaceDto> GetByIdAsync(Guid workplaceId, Guid companyId)
        {
            var query = from w in _context.Workplaces
                        where w.Id == workplaceId && w.CompanyId == companyId
                        select new
                        {
                            Workplace = w
                        };
            var result = await query.Select(o => new WorkplaceDto
            {
                Id = o.Workplace.Id,
                Name = o.Workplace.Name.Value,
                Address = new WorkplaceAddressDto
                {
                    ZipCode = o.Workplace.Address.ZipCode,
                    Street = o.Workplace.Address.Street,
                    Number = o.Workplace.Address.Number,
                    Complement = o.Workplace.Address.Complement,
                    Neighborhood = o.Workplace.Address.Neighborhood,
                    City = o.Workplace.Address.City,
                    State = o.Workplace.Address.State,
                    Country = o.Workplace.Address.Country
                },
            }).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Workplace), workplaceId.ToString()));
            return result;
        }

        public async Task<IEnumerable<WorkplaceDto>> GetAllAsync(Guid companyId)
        {
            var query = from w in _context.Workplaces
                        where w.CompanyId == companyId
                        select new
                        {
                            Workplace = w
                        };

            var result = await query.Select(o => new WorkplaceDto
            {
                Id = o.Workplace.Id,
                Name = o.Workplace.Name.Value,
                Address = new WorkplaceAddressDto
                {
                    ZipCode = o.Workplace.Address.ZipCode,
                    Street = o.Workplace.Address.Street,
                    Number = o.Workplace.Address.Number,
                    Complement = o.Workplace.Address.Complement,
                    Neighborhood = o.Workplace.Address.Neighborhood,
                    City = o.Workplace.Address.City,
                    State = o.Workplace.Address.State,
                    Country = o.Workplace.Address.Country
                },
            }).ToArrayAsync();
            return result;
        }
    }
}
