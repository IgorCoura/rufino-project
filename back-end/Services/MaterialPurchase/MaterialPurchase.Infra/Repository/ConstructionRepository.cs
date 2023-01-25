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
    public class ConstructionRepository : BaseRepository<Construction>, IConstructionRepository
    {
        public ConstructionRepository(BaseContext context) : base(context)
        {
        }
    }
}
