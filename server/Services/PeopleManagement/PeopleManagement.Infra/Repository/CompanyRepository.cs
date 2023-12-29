using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Infra.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        public CompanyRepository(PeopleManagementContext context) : base(context)
        {
            
        }
    }
}
