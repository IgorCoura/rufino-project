using AutoMapper;
using BuildManagement.Domain.Entities;
using BuildManagement.Domain.Models.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildManagement.Service.Mappers
{
    public class EntityToModelProfile : Profile
    {
        public EntityToModelProfile()
        {
            CreateMap<Provider, ProviderModel>();
          
        }
    }
}
