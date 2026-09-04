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
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Options;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.WorkloadCalendar;
using PeopleManagement.Domain.Options;
using PeopleManagement.Domain.Services;

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
            service.AddScoped<IRecoverComplementaryInfoToDocumentTemplateService, RecoverComplementaryInfoToDocumentTemplateService>();

#pragma warning disable CS0618 // Archive: feature descontinuada, mantida so para o dado ja gravado nao ficar orfao. Ver o [Obsolete] nos tipos.
            service.AddScoped<IArchiveService, ArchiveService>();
#pragma warning restore CS0618
            service.AddScoped<ICompleteAdmissionService, CompleteAdmissionService>();
            service.AddScoped<IDocumentService, DocumentService>();
#pragma warning disable CS0618 // Archive: feature descontinuada, mantida so para o dado ja gravado nao ficar orfao. Ver o [Obsolete] nos tipos.
            service.AddScoped<IArchiveCategoryService, ArchiveCategoryService>();
#pragma warning restore CS0618
            service.AddScoped<ISignDocumentService, SignDocumentService>();
            service.AddScoped<IDocumentDepreciationService, DocumentDepreciationService>();
            service.AddScoped<IDocumentSignatureReminderService, DocumentSignatureReminderService>();
            service.AddScoped<IWhatsAppQueueService, WhatsAppQueueService>();
            service.AddScoped<HangfireJobRegister>();
            service.AddScoped<IRecurringDocumentService, RecurringDocumentService>();

            // Domain Services
            service.AddScoped<IEmployeeDocumentStatusService, EmployeeDocumentStatusService>();
            service.AddSingleton<IHolidayProvider, BrazilianHolidayProvider>();
            service.AddSingleton<IWorkloadCalendarService, WorkloadCalendarService>();

            var documentTemplatesOptions = new DocumentTemplatesOptions();
            configuration.GetSection(DocumentTemplatesOptions.ConfigurationSection).Bind(documentTemplatesOptions);
            service.AddSingleton(documentTemplatesOptions);

            var documentOptions = new DocumentOptions();
            configuration.GetSection(DocumentOptions.ConfigurationSection).Bind(documentOptions);
            service.AddSingleton(documentOptions);

            var signingServiceAccountOptions = new SigningServiceAccountOptions();
            configuration.GetSection(SigningServiceAccountOptions.ConfigurationSection).Bind(signingServiceAccountOptions);
            service.AddSingleton(signingServiceAccountOptions);

            var signOptions = new SignatureProviderOptions();
            configuration.GetSection(SignatureProviderOptions.ConfigurationSection).Bind(signOptions);
            service.AddSingleton(signOptions);

            var timeZoneOptions = new TimeZoneOptions();
            configuration.GetSection(TimeZoneOptions.SectionName).Bind(timeZoneOptions);
            service.AddSingleton(timeZoneOptions);

            service.Configure<MessagingQueueOptions>(configuration.GetSection(MessagingQueueOptions.SectionName));

            return service;
        }
    }
}

