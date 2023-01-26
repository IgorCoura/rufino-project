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
    public class EntityToEntityProfile : Profile
    {
        EntityToEntityProfile()
        {
            CreateMap<Purchase, PurchaseResponse>();
        }
    }
    
}
