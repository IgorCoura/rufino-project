using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.options;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.SecurityDocumentAggregate.Options;
using PeopleManagement.Services.Services;
using PeopleManagement.Services.Services.RecoverInfoToSecurityDocument;

namespace PeopleManagement.API.DependencyInjection
{
    public static class ServicesInjectionConfig
    {
        public static IServiceCollection AddServicesDependencies(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddScoped<IRecoverNR01InfoToSecurityDocumentService, RecoverNR01InfoToSecurityDocumentService>();

            service.AddScoped<IArchiveService, ArchiveService>();
            service.AddScoped<ICompleteAdmissionService, CompleteAdmissionService>();
            service.AddScoped<ISecurityDocumentService, SecurityDocumentService>();

            var securityDocumentsFilesOptions = new SecurityDocumentsFilesOptions();
            configuration.Bind("SecurityDocumentsFilesOptions", securityDocumentsFilesOptions);
            service.AddSingleton(securityDocumentsFilesOptions);

            var documentTemplatesOptions = new DocumentTemplatesOptions();
            configuration.Bind("DocumentTemplatesOptions", documentTemplatesOptions);
            service.AddSingleton(documentTemplatesOptions);

            return service;
        }
    }
}

