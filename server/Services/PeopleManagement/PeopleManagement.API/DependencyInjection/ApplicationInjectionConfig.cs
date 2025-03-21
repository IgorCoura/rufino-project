using PeopleManagement.Application.Queries.Company;
using PeopleManagement.Application.Queries.Department;
using PeopleManagement.Application.Queries.Employee;
using PeopleManagement.Application.Queries.Position;
using PeopleManagement.Application.Queries.Role;
using PeopleManagement.Application.Queries.Workplace;

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
            service.AddScoped<IRoleQueries, RoleQueries>();
            service.AddScoped<IDepartmentQueries, DepartmentQueries>();
            service.AddScoped<IPositionQueries, PositionQueries>();
            service.AddScoped<IWorkplaceQueries, WorkplaceQueries>();

            return service;
        }
    }
}
