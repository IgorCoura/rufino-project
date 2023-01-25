using Commom.Infra.Base;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Infra.Repository
{
    public class PurchaseRepository : BaseRepository<Purchase>, IPurchaseRepository
    {
        public PurchaseRepository(BaseContext context) : base(context)
        {
        }
    }
}
