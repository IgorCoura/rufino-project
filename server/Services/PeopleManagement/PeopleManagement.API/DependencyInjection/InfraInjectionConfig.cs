using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Infra.Repository;

namespace PeopleManagement.API.DependencyInjection
{
    public static class InfraInjectionConfig
    {
        public static IServiceCollection AddInfraDependencies(this IServiceCollection service, IConfiguration configuration)
        {

            service.AddScoped<ICompanyRepository, CompanyRepository>();

            return service;
        }
    }
}
