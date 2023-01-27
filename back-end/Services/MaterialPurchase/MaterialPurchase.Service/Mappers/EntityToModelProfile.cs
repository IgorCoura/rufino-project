using AutoMapper;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Models;
using MaterialPurchase.Domain.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Service.Mappers
{
    public class EntityToModelProfile : Profile
    {
        public EntityToModelProfile()
        {
            CreateMap<Purchase, PurchaseResponse>();
            CreateMap<Purchase, SimplePurchaseResponse>();
            CreateMap<Construction, ConstructionResponse>();
            CreateMap<Provider, ProviderResponse>();
        }
    }
    
}
