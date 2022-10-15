using AutoMapper;
using BuildManagement.Domain.Entities;
using BuildManagement.Domain.Entities.Purchase;
using BuildManagement.Domain.Models.Brand;
using BuildManagement.Domain.Models.Construction;
using BuildManagement.Domain.Models.Material;
using BuildManagement.Domain.Models.Purchase.MaterialPurchase;
using BuildManagement.Domain.Models.Provider;
using BuildManagement.Domain.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuildManagement.Domain.Models.Purchase.CreateMaterialPurchase;

namespace BuildManagement.Service.Mappers
{
    public class ModelToEntityProfile : Profile
    {
        public ModelToEntityProfile()
        {
            CreateMap<CreateProviderModel, Provider>();
            CreateMap<CreateMaterialModel, Material>();
            CreateMap<CreateConstructionModel, Construction>();
            CreateMap<CreateBrandModel, Brand>();
            CreateMap<CreateUserModel, User>();

            #region MaterialPurchase

            #region CreateMaterialPurchase

            CreateMap<CreateMaterialPurchaseRequest, MaterialPurchase>();
            CreateMap<CreateItemMaterialPurchaseRequest, ItemMaterialPurchase>();

            #endregion

            #endregion
        }
    }
}
