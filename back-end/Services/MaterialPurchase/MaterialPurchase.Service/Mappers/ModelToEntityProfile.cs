using AutoMapper;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Service.Mappers
{
    public class ModelToEntityProfile : Profile
    {
        public ModelToEntityProfile() 
        {
            CreateMap<CreateDraftPurchaseRequest, Purchase>();
            CreateMap<DraftPurchaseRequest, Purchase>();
            CreateMap<CreateMaterialDraftPurchaseRequest, ItemMaterialPurchase>();
            CreateMap<MaterialDraftPurchaseRequest, ItemMaterialPurchase>();
        }
    }
}
