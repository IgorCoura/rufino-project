using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Services.Services;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate.Interfaces;
using PeopleManagement.Services.HangfireJobRegistrar;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using PeopleManagement.Services.Services.RecoverInfoToDocument;

namespace PeopleManagement.API.DependencyInjection
{
    public static class ServicesInjectionConfig
    {
        public static IServiceCollection AddServicesDependencies(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddScoped<IRecoverCompanyInfoToDocumentTemplateService, RecoverCompanyInfoToDocumentTemplateService>();
            service.AddScoped<IRecoverDepartamentInfoToDocumentTemplateService, RecoverDepartamentInfoToDocumentTemplateService>();
            service.AddScoped<IRecoverEmployeeInfoToDocumentTemplateService, RecoverEmployeeInfoToDocumentTemplateService>();
            service.AddScoped<IRecoverPGRInfoToDocumentTemplateService, RecoverPGRInfoToDocumentTemplateService>();
            service.AddScoped<IRecoverPositionInfoToDocumentTemplateService, RecoverPositionInfoToDocumentTemplateService>();
            service.AddScoped<IRecoverRoleInfoToDocumentTemplateService, RecoverRoleInfoToDocumentTemplateService>();
            service.AddScoped<IRecoverWorkplaceInfoToDocumentTemplateService, RecoverWorkplaceInfoToDocumentTemplateService>();

            service.AddScoped<IArchiveService, ArchiveService>();
            service.AddScoped<ICompleteAdmissionService, CompleteAdmissionService>();
            service.AddScoped<IDocumentService, DocumentService>();
            service.AddScoped<IArchiveCategoryService, ArchiveCategoryService>();
            service.AddScoped<ISignDocumentService, SignDocumentService>();
            service.AddScoped<IDocumentDepreciationService, DocumentDepreciationService>();
            service.AddScoped<HangfireJobRegister>();
            service.AddScoped<IRecurringDocumentService, RecurringDocumentService>();

            var documentTemplatesOptions = new DocumentTemplatesOptions();
            configuration.Bind("DocumentTemplatesOptions", documentTemplatesOptions);
            service.AddSingleton(documentTemplatesOptions);

            return service;
        }
    }
}

