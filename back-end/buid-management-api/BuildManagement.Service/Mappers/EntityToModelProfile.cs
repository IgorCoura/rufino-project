using AutoMapper;
using BuildManagement.Domain.Entities;
using BuildManagement.Domain.Entities.Purchase;
using BuildManagement.Domain.Models.Brand;
using BuildManagement.Domain.Models.Construction;
using BuildManagement.Domain.Models.Material;
using BuildManagement.Domain.Models.Purchase.MaterialPurchase;
using BuildManagement.Domain.Models.Provider;
using BuildManagement.Domain.Models.User;
using BuildManagement.Domain.Models.Purchase.MaterialReceive;
using BuildManagement.Domain.Models.Purchase.CreateMaterialPurchase;

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
            CreateMap<User, UserModel>();
            CreateMap<MaterialPurchase, ReturnMaterialPurchaseModel>()
                .ForMember(x => x.Status, m => m.MapFrom(req => req.Status.ToString()));


            #region MaterialPurchase

            #region MaterialReceiveMap
            CreateMap<MaterialPurchase, MaterialReceiveResponse>()
                .ForMember(x => x.Status, m => m.MapFrom(req => req.Status.ToString()));
            CreateMap<ItemMaterialPurchase, ItemMaterialPurchaseResponse>()
                .ForMember(x => x.Status, m => m.MapFrom(req => req.Status.ToString()));
            #endregion

            #region CreateMaterialPurchase

            CreateMap<MaterialPurchase, CreateMaterialPurchaseResponse>();
            CreateMap<ItemMaterialPurchase, CreateItemMaterialPurchaseResponse>();

            #endregion

            #endregion
        }
    }
}
