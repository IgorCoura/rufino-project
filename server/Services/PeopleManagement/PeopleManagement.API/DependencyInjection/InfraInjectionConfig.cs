using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Infra.Repository;

namespace PeopleManagement.API.DependencyInjection
{
    public static class InfraInjectionConfig
    {
        public static IServiceCollection AddInfraDependencies(this IServiceCollection service, IConfiguration configuration)
        {

            service.AddScoped<ICompanyRepository, CompanyRepository>();
            service.AddScoped<IArchiveRepository, ArchiveRepository>();
            service.AddScoped<IEmployeeRepository, EmployeeRepository>();

            return service;
        }
    }
}
