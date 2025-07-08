using PeopleManagement.Application.Queries.ArchiveCategory;
using PeopleManagement.Application.Queries.Company;
using PeopleManagement.Application.Queries.Department;
using PeopleManagement.Application.Queries.Document;
using PeopleManagement.Application.Queries.DocumentTemplate;
using PeopleManagement.Application.Queries.Employee;
using PeopleManagement.Application.Queries.Position;
using PeopleManagement.Application.Queries.RequireDocuments;
using PeopleManagement.Application.Queries.Role;
using PeopleManagement.Application.Queries.Workplace;
using PeopleManagement.Domain.ErrorTools;

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
            service.AddScoped<IArchiveCategoryQueries, ArchiveCategoryQueries>();
            service.AddScoped<IDocumentTemplateQueries, DocumentTemplateQueries>();
            service.AddScoped<IRequireDocumentsQueries, RequireDocumentsQueries>();
            service.AddScoped<IDocumentQueries, DocumentQueries>();

            return service;
        }
    }
}
