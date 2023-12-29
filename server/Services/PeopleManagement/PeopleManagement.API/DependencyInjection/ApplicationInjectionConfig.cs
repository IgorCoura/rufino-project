using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PeopleManagement.Application.Behaviors;
using PeopleManagement.Application.Validations;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate;
using PeopleManagement.Infra.Repository;

namespace PeopleManagement.API.DependencyInjection
{
    public static class ApplicationInjectionConfig
    {
        public static IServiceCollection AddApplicationDependencies(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddValidatorsFromAssemblyContaining<ValidatorAssembly>();
            service.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));

            return service;
        }
    }
}
