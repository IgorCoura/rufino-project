using FluentValidation;
using MaterialControl.Domain.Interfaces;
using MaterialControl.Infra.Repository;
using MaterialControl.Services.Mappers;
using MaterialControl.Services.Services;
using MaterialControl.Services.Validations;

namespace MaterialControl.API.Configurations
{
    public static class DependencyInjectionConfig
    {
        public static IServiceCollection AddDependencies(this IServiceCollection service, IConfiguration configuration)
        {
            #region Repositories

            service.AddScoped<IBrandRepository, BrandRepository>();
            service.AddScoped<IMaterialRepository, MaterialRepository>();
            service.AddScoped<IUnityRepository, UnityRepository>();

            #endregion

            #region Services

            service.AddScoped<IBrandService, BrandService>();
            service.AddScoped<IMaterialService, MaterialService>();
            service.AddScoped<IUnityService, UnityService>();

            #endregion

            #region Options



            #endregion

            #region Validators

            service.AddValidatorsFromAssemblyContaining(typeof(CreateBrandValidator));

            #endregion

            service.AddAutoMapper(typeof(EntityToModelProfile), typeof(ModelToEntityProfile), typeof(EntityToMessageProfile));

            return service;
        }
    }
}
