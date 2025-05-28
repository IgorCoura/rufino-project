using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Services.Services;
using PeopleManagement.Services.Services.RecoverInfoToSecurityDocument;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate.Interfaces;
using Hangfire;
using PeopleManagement.Services.HangfireJobRegistrar;

namespace PeopleManagement.API.DependencyInjection
{
    public static class ServicesInjectionConfig
    {
        public static IServiceCollection AddServicesDependencies(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddScoped<IRecoverNR01InfoToDocumentTemplateService, RecoverNR01InfoToDocumentTemplateService>();

            service.AddScoped<IArchiveService, ArchiveService>();
            service.AddScoped<ICompleteAdmissionService, CompleteAdmissionService>();
            service.AddScoped<IDocumentService, DocumentService>();
            service.AddScoped<IArchiveCategoryService , ArchiveCategoryService>();
            service.AddScoped<ISignDocumentService , SignDocumentService>();
            service.AddScoped<IDocumentDepreciationService , DocumentDepreciationService>();
            service.AddScoped<HangfireJobRegister>();


            var documentTemplatesOptions = new DocumentTemplatesOptions();
            configuration.Bind("DocumentTemplatesOptions", documentTemplatesOptions);
            service.AddSingleton(documentTemplatesOptions);

            return service;
        }
    }
}

