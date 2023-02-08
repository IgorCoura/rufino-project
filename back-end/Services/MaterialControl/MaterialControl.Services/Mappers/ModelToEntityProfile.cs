using AutoMapper;
using MaterialControl.Domain.Entities;
using MaterialControl.Domain.Models.Request;

namespace MaterialControl.Services.Mappers
{
    public class ModelToEntityProfile : Profile
    {
        public ModelToEntityProfile()
        {
            CreateMap<CreateBrandRequest, Brand>();
            CreateMap<CreateMaterialRequest, Material>();
            CreateMap<CreateUnityRequest, Unity>();
            CreateMap<BrandRequest, Brand>();
            CreateMap<MaterialRequest, Material>();
            CreateMap<UnityRequest, Unity>();
        }
    }
}

