using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Application.Queries.Company
{
    public class CompanyQueries(PeopleManagementContext peopleManagementContext) : ICompanyQueries
    {
        private PeopleManagementContext _context = peopleManagementContext;


        public async Task<IEnumerable<CompanySimplefiedDTO>> GetCompaniesSimplefiedsAsync(Guid[] companiesIds)
        {
            var query = _context.Companies.AsNoTracking().Where(x => companiesIds.Contains(x.Id));


            var companies = await query.Select(c => new CompanySimplefiedDTO
            {
                Id = c.Id,
                CorporateName = c.CorporateName.Value,
                FantasyName = c.FantasyName.Value,
                Cnpj = c.Cnpj.Value
            }).ToArrayAsync();

            return companies;
        }

        public async Task<CompanySimplefiedDTO> GetCompanySimplefiedAsync(Guid id)
        {
            var query = _context.Companies.AsNoTracking().Where(x => x.Id == id);


            var companies = await query.Select(c => new CompanySimplefiedDTO
            {
                Id = c.Id,
                CorporateName = c.CorporateName.Value,
                FantasyName = c.FantasyName.Value,
                Cnpj = c.Cnpj.Value
            }).FirstOrDefaultAsync()
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Company), id.ToString()));

            return companies;
        }
    }
}
