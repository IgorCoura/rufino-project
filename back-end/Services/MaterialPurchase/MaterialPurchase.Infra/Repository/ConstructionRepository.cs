﻿using Commom.Infra.Base;
using MaterialPurchase.Domain.BaseEntities;
using MaterialPurchase.Domain.Interfaces;
using MaterialPurchase.Infra.Context;

namespace MaterialPurchase.Infra.Repository
{
    public class ConstructionRepository : BaseRepository<Construction>, IConstructionRepository
    {
        public ConstructionRepository(MaterialPurchaseContext context) : base(context)
        {
    
        }
    }
}
