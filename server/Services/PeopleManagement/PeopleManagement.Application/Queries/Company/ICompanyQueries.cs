using Microsoft.EntityFrameworkCore;

namespace PeopleManagement.Application.Queries.Company
{
    public interface  ICompanyQueries
    {
        Task<IEnumerable<CompanySimplefiedDTO>> GetCompaniesSimplefiedsAsync(Guid[] companiesIds);
        Task<CompanySimplefiedDTO> GetCompanySimplefiedAsync(Guid id);
    }
}
