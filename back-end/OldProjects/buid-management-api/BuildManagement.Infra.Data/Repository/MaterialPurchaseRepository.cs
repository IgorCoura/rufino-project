using BuildManagement.Domain.Entities.Purchase;
using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Infra.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Infra.Data.Repository
{
    public class MaterialPurchaseRepository : BaseRepository<MaterialPurchase>, IMaterialPurchaseRepository
    {
        public MaterialPurchaseRepository(ApplicationContext context) : base(context)
        {
        }
    }
}
