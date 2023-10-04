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
            CreateMap<PurchaseAuthUserGroup, AuthUserGroupResponse>();
            CreateMap<ConstructionAuthUserGroup, AuthUserGroupResponse>();
            CreateMap<PurchaseUserAuthorization, UserAuthorizationResponse>();   
            CreateMap<ConstructionUserAuthorization, UserAuthorizationResponse>();
            CreateMap<Brand, BrandResponse>();
            CreateMap<Purchase, CompletePurchaseResponse>();
            CreateMap<Construction, ConstructionResponse>();
            CreateMap<ItemMaterialPurchase, ItemMaterialPurchaseResponse>();
            CreateMap<Material, MaterialResponse>();
            CreateMap<Provider, ProviderResponse>();
            CreateMap<PurchaseDeliveryItem, PurchaseDeliveryItemResponse>();
            CreateMap<Purchase, PurchaseResponse>();
            CreateMap<Purchase, PurchaseWithMaterialResponse>();
            CreateMap<Purchase, SimplePurchaseResponse>();
            CreateMap<User, UserResponse>();
            CreateMap<Company, CompanyResponse>();

        }
    }
    
}
