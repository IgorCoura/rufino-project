using FluentValidation;
using PeopleManagement.Application.Behaviors;
using PeopleManagement.Application.Queries.Employee;
using PeopleManagement.Application.Validations;

namespace PeopleManagement.API.DependencyInjection
{
    public static class ApplicationInjectionConfig
    {
        public static IServiceCollection AddApplicationDependencies(this IServiceCollection service, IConfiguration configuration)
        {
            //service.AddValidatorsFromAssemblyContaining<ValidatorAssembly>();
            //service.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));

            service.AddScoped<IEmployeeQueries, EmployeeQueries>();

            return service;
        }
    }
}
