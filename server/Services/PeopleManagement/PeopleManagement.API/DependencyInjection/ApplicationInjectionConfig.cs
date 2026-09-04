using PeopleManagement.Application.Queries.ArchiveCategory;
using PeopleManagement.Application.Queries.Company;
using PeopleManagement.Application.Queries.Department;
using PeopleManagement.Application.Queries.Document;
using PeopleManagement.Application.Queries.DocumentGroup;
using PeopleManagement.Application.Queries.DocumentTemplate;
using PeopleManagement.Application.Queries.Employee;
using PeopleManagement.Application.Queries.Position;
using PeopleManagement.Application.Queries.RequireDocuments;
using PeopleManagement.Application.Queries.Role;
using PeopleManagement.Application.Queries.BatchDocument;
using PeopleManagement.Application.Queries.BatchDownload;
using PeopleManagement.Application.Queries.DocumentDashboard;
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
#pragma warning disable CS0618 // Archive: feature descontinuada, mantida so para o dado ja gravado nao ficar orfao. Ver o [Obsolete] nos tipos.
            service.AddScoped<IArchiveCategoryQueries, ArchiveCategoryQueries>();
#pragma warning restore CS0618
            service.AddScoped<IDocumentTemplateQueries, DocumentTemplateQueries>();
            service.AddScoped<IRequireDocumentsQueries, RequireDocumentsQueries>();
            service.AddScoped<IDocumentQueries, DocumentQueries>();
            service.AddScoped<IDocumentGroupQueries, DocumentGroupQueries>();
            service.AddScoped<IBatchDocumentQueries, BatchDocumentQueries>();
            service.AddScoped<IBatchDownloadQueries, BatchDownloadQueries>();
            service.AddScoped<IDocumentDashboardQueries, DocumentDashboardQueries>();

            return service;
        }
    }
}
