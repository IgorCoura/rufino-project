using AutoMapper;
using Commom.MessageBroker.Message;
using MaterialControl.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterialControl.Services.Mappers
{
    public class EntityToMessageProfile : Profile
    {
        public EntityToMessageProfile()
        {
            CreateMap<Brand, ModifyBrandMessage>();
            CreateMap<Brand, DeleteBrandMessage>();
            CreateMap<Material, ModifyMaterialMessage>()
                .ForMember(x => x.Unity, m => m.MapFrom(req => req.Unity!.Name));
            CreateMap<Material, DeleteMaterialMessage>();
        }
    }
}
