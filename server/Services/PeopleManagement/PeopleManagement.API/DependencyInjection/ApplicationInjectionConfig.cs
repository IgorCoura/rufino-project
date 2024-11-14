using PeopleManagement.Application.Commands.Queries.Company;
using PeopleManagement.Application.Commands.Queries.Employee;

namespace PeopleManagement.API.DependencyInjection
{
    public static class ApplicationInjectionConfig
    {
        public static IServiceCollection AddApplicationDependencies(this IServiceCollection service, IConfiguration configuration)
        {
            //service.AddValidatorsFromAssemblyContaining<ValidatorAssembly>();
            //service.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));

            service.AddScoped<IEmployeeQueries, EmployeeQueries>();
            service.AddScoped<ICompanyQueries, CompanyQueries>();

            return service;
        }
    }
}
