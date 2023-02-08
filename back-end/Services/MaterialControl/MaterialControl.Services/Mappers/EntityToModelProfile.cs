using AutoMapper;
using MaterialControl.Domain.Entities;
using MaterialControl.Domain.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControl.Services.Mappers
{
    public class EntityToModelProfile : Profile
    {
        public EntityToModelProfile()
        {
            CreateMap<Brand, BrandResponse>();
            CreateMap<Material, MaterialResponse>();
            CreateMap<Unity, UnityResponse>();
        }
    }
}
