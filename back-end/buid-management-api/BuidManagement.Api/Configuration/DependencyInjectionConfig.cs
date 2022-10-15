using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Domain.Options;
using BuildManagement.Infra.Data.Repository;
using BuildManagement.Service.Services;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using BuildManagement.Service.Validations;
using BuildManagement.Domain.Models.Brand;
using FluentValidation;
using BuildManagement.Domain.Models.Material;
using BuildManagement.Domain.Models.Provider;
using System.Reflection;

namespace BuidManagement.Api.Configuration
{
    public static class DependencyInjectionConfig
    {
        public static IServiceCollection ResolveDependencies(this IServiceCollection service, ConfigurationManager configuration)
        {
            //Repository
            service.AddScoped<IProviderRepository, ProviderRepository>();
            service.AddScoped<IMaterialRepository, MaterialRepository>();
            service.AddScoped<IConstructionRepository, ConstructionRepository>();
            service.AddScoped<IBrandRepository, BrandRepository>();
            service.AddScoped<IMaterialPurchaseRepository, MaterialPurchaseRepository>();
            service.AddScoped<IUserRepository, UserRepository>();
            service.AddScoped<IAuthService, AuthService>();

            //Service
            service.AddScoped<IProviderService, ProviderService>();
            service.AddScoped<IMaterialService, MaterialService>();
            service.AddScoped<IConstructionService, ConstructionService>();
            service.AddScoped<IBrandService, BrandService>();
            service.AddScoped<IMaterialPurchaseService, MaterialPurchaseService>();
            service.AddScoped<IUserService, UserService>();

            //Options
            service.Configure<TokenGeneratorOptions>(configuration.GetSection("Jwt"));
            service.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

            //validations
            service.AddValidatorsFromAssembly(Assembly.GetAssembly(typeof(AssemblyValidators)));

            return service;
        }
    }
}
