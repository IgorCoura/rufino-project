using Azure.Storage.Blobs;
using Microsoft.Net.Http.Headers;
using Polly;
using PeopleManagement.Domain.AggregatesModel.ArchiveAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.CompanyAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DepartmentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.PositionAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RoleAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Infra.Repository;
using PeopleManagement.Infra.Services;
using PeopleManagement.Domain.AggregatesModel.ArchiveCategoryAggregate.Interfaces;
using PeopleManagement.Infra.Idempotency;
using PeopleManagement.Domain.AggregatesModel.WorkplaceAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.DocumentGroupAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.WebHookAggregate;
using PeopleManagement.Infra.Policies;

namespace PeopleManagement.API.DependencyInjection
{
    public static class InfraInjectionConfig
    {
        public static IServiceCollection AddInfraDependencies(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddScoped<IRequestManager, RequestManager>();

            service.AddScoped<IArchiveCategoryRepository, ArchiveCategoryRepository>();
            service.AddScoped<IArchiveRepository, ArchiveRepository>();
            service.AddScoped<ICompanyRepository, CompanyRepository>();
            service.AddScoped<IDepartmentRepository, DepartamentRepository>();
            service.AddScoped<IDocumentTemplateRepository, DocumentTemplateRepository>();
            service.AddScoped<IEmployeeRepository, EmployeeRepository>();
            service.AddScoped<IPositionRepository, PositionRepository>();
            service.AddScoped<IRequireDocumentsRepository, RequireDocumentsRepository>();
            service.AddScoped<IRoleRepository, RoleRepository>();
            service.AddScoped<IDocumentRepository, DocumentRepository>();
            service.AddScoped<IWorkplaceRepository, WorkplaceRepository>();
            service.AddScoped<IDocumentGroupRepository, DocumentGroupRepository>();
            service.AddScoped<IWebHookRepository, WebHookRepository>();

            service.AddScoped<IPdfService, PdfService>();
            service.AddScoped<IBlobService, BlobAzureService>();
            service.AddScoped<ILocalStorageService, LocalStorageService>();
            service.AddScoped<IFileDownloadService, FileDownloadService>();

            service.AddSingleton(x => new BlobServiceClient(configuration.GetConnectionString("BlobStorage")));

            service.AddHttpClient<IDocumentSignatureService, ZapSignDocumentSignatureService>((serviceProvider, httpClient) =>
            {
                httpClient.BaseAddress = new Uri(configuration.GetSection("ZapSign")["URI"]!);
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, "Bearer " + configuration.GetSection("ZapSign")["Token"]!);
                httpClient.Timeout = TimeSpan.FromMinutes(2);
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ZapSignDocumentSignatureService>>();
                var context = new Polly.Context { ["Logger"] = logger };

                return HttpPolicyFactory.GetCombinedPolicy(retryCount: 6, timeoutSeconds: 30);
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            service.AddHttpClient<IWebHookManagementService, ZapSignWebHookManagementService>((serviceProvider, httpClient) =>
            {
                httpClient.BaseAddress = new Uri(configuration.GetSection("ZapSign")["URI"]!);
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, "Bearer " + configuration.GetSection("ZapSign")["Token"]!);
                httpClient.Timeout = TimeSpan.FromMinutes(2);
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<ZapSignWebHookManagementService>>();
                var context = new Polly.Context { ["Logger"] = logger };

                return Policy.WrapAsync(
                    HttpPolicyFactory.GetCircuitBreakerPolicy(),
                    HttpPolicyFactory.GetAggressiveRetryPolicy(retryCount: 16));
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            service.AddHttpClient<IAuthorizationService, AuthorizationService>((serviceProvider, httpClient) =>
            {
                httpClient.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<AuthorizationService>>();
                var context = new Polly.Context { ["Logger"] = logger };

                return HttpPolicyFactory.GetCombinedPolicy(retryCount: 3, timeoutSeconds: 30);
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            return service;
        }
    }
}
