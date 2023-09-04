using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Interfaces.Repositories;
using MaterialPurchase.Infra.Context;

namespace MaterialPurchase.Infra.Repository
{
    public class CompanyRepository : BaseRepository<Company>, ICompanyRepository
    {
        public CompanyRepository(MaterialPurchaseContext context) : base(context)
        {
        }
    }
}
