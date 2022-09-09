using AutoMapper;
using BuildManagement.Domain.Entities;
using BuildManagement.Domain.Entities.Purchase;
using BuildManagement.Domain.Models.Brand;
using BuildManagement.Domain.Models.Construction;
using BuildManagement.Domain.Models.Material;
using BuildManagement.Domain.Models.Purchase.MaterialPurchase;
using BuildManagement.Domain.Models.Purchase.ItemMaterialPurchase;
using BuildManagement.Domain.Models.Provider;
using BuildManagement.Domain.Models.User;

namespace BuildManagement.Service.Mappers
{
    public class EntityToModelProfile : Profile
    {
        public EntityToModelProfile()
        {
            CreateMap<Provider, ProviderModel>();
            CreateMap<Material, MaterialModel>();
            CreateMap<Construction, ConstructionModel>();
            CreateMap<Brand, BrandModel>();
            CreateMap<MaterialPurchase, ReturnCreateMaterialPurchaseModel>();
            CreateMap<ItemMaterialPurchase, MaterialItemPurchaseModel>();
            CreateMap<User, UserModel>();
        }
    }
}
