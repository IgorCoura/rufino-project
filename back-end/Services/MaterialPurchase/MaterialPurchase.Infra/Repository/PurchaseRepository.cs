using Commom.Infra.Base;
using MaterialPurchase.Domain.BaseEntities;
using MaterialPurchase.Domain.Interfaces;
using MaterialPurchase.Infra.Context;

namespace MaterialPurchase.Infra.Repository
{
    public class PurchaseRepository : BaseRepository<Purchase>, IPurchaseRepository
    {
        public PurchaseRepository(MaterialPurchaseContext context) : base(context)
        {
        }
    }
}
