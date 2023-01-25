using AutoMapper;
using MaterialPurchase.Domain.Entities;
using MaterialPurchase.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialPurchase.Service.Mappers
{
    public class ModelToEntityProfile : Profile
    {
        ModelToEntityProfile() 
        {
            CreateMap<CreateDraftPurchaseRequest, Purchase>();
            CreateMap<DraftPurchaseRequest, Purchase>();
            CreateMap<MaterialDraftPurchaseRequest, ItemMaterialPurchase>();
        }
    }
}
