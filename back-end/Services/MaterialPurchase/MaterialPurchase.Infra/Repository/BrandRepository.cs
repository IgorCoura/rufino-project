using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Interfaces.Repositories;
using MaterialPurchase.Infra.Context;

namespace MaterialPurchase.Infra.Repository
{
    public class BrandRepository : BaseRepository<Brand>, IBrandRepository
    {
        public BrandRepository(MaterialPurchaseContext context) : base(context)
        {
        }
    }
}
