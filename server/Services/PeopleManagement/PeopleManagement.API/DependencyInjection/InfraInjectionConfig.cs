using Azure.Storage.Blobs;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RequireSecurityDocumentsAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using PeopleManagement.Infra.Repository;
using PeopleManagement.Infra.Services;

namespace PeopleManagement.API.DependencyInjection
{
    public static class InfraInjectionConfig
    {
        public static IServiceCollection AddInfraDependencies(this IServiceCollection service, IConfiguration configuration)
        {            

            service.AddScoped<IArchiveRepository, ArchiveRepository>();
            service.AddScoped<ICompanyRepository, CompanyRepository>();
            service.AddScoped<IDepartmentRepository, DepartamentRepository>();
            service.AddScoped<IDocumentTemplateRepository, DocumentTemplateRepository>();
            service.AddScoped<IEmployeeRepository, EmployeeRepository>();
            service.AddScoped<IPositionRepository, PositionRepository>();
            service.AddScoped<IRequireSecurityDocumentsRepository, RequireSecurityDocumentsRepository>();
            service.AddScoped<IRoleRepository, RoleRepository>();
            service.AddScoped<ISecurityDocumentRepository, SecurityDocumentRepository>();

            service.AddScoped<IPdfService, PdfService>();
            service.AddScoped<IBlobService, BlobAzureService>();
            service.AddScoped<ILocalStorageService, LocalStorageService>();

            service.AddSingleton(x => new BlobServiceClient(configuration.GetConnectionString("BlobStorage")));

            return service;
        }
    }
}
