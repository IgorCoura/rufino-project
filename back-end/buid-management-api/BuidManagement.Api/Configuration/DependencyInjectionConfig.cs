using BuildManagement.Domain.Interfaces.Repository;
using BuildManagement.Domain.Interfaces.Services;
using BuildManagement.Infra.Data.Repository;
using BuildManagement.Service.Services;

namespace BuidManagement.Api.Configuration
{
    public static class DependencyInjectionConfig
    {
        public static IServiceCollection ResolveDependencies(this IServiceCollection service, ConfigurationManager configuration)
        {
            //Repository
            service.AddScoped<IProviderRepository, ProviderRepository>();

            //Service
            service.AddScoped<IProviderService, ProviderService>();


            return service;
        }
    }
}
