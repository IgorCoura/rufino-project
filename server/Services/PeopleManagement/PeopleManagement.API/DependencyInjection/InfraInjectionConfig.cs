using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;
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
using PeopleManagement.Domain.Services;
using PeopleManagement.Domain.Options;
using PeopleManagement.Services.Services;

namespace PeopleManagement.API.DependencyInjection
{
    public static class InfraInjectionConfig
    {
        public static IServiceCollection AddInfraDependencies(this IServiceCollection service, IConfiguration configuration)
        {
            service.AddScoped<IRequestManager, RequestManager>();

#pragma warning disable CS0618 // Archive: feature descontinuada, mantida so para o dado ja gravado nao ficar orfao. Ver o [Obsolete] nos tipos.
            service.AddScoped<IArchiveCategoryRepository, ArchiveCategoryRepository>();
            service.AddScoped<IArchiveRepository, ArchiveRepository>();
#pragma warning restore CS0618
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
            service.AddSingleton<IBrowserProvider, BrowserProvider>();
            service.AddScoped<IBlobService, BlobS3Service>();
            service.AddScoped<ILocalStorageService, LocalStorageService>();
            service.AddScoped<IFileDownloadService, FileDownloadService>();
            service.AddScoped<IWhatsAppHealthCheckService, WhatsAppHealthCheckService>();

            // Configure S3 Options
            service.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

            service.AddSingleton<IAmazonS3>(sp =>
            {
                var s3Options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;

                // Falha aqui dizendo O QUE falta. Sem esta guarda, credencial ausente estoura lá
                // dentro do SDK da AWS como ArgumentNullException em 'awsSecretAccessKey' — e não
                // no arranque, mas na PRIMEIRA requisição que resolve um controller, porque este
                // singleton é preguiçoso. Foi o que aconteceu em 2026-09-04, quando a seção passou
                // de "S3" para "Storage" e a chave do 'dotnet user-secrets' ficou órfã: a mensagem
                // não dizia nem qual configuração, nem que ela havia sido renomeada.
                if (string.IsNullOrWhiteSpace(s3Options.AccessKey) || string.IsNullOrWhiteSpace(s3Options.SecretKey))
                {
                    throw new InvalidOperationException(
                        $"Armazenamento nao configurado: '{StorageOptions.SectionName}:AccessKey' e "
                        + $"'{StorageOptions.SectionName}:SecretKey' precisam de valor. O segredo NAO vive no "
                        + "appsettings versionado — em desenvolvimento vem do 'dotnet user-secrets', em producao "
                        + $"da variavel de ambiente {StorageOptions.SectionName}__SecretKey. "
                        + "A secao chamava-se 'S3' ate 2026-09-04; se o valor foi configurado antes disso, "
                        + "ele ficou na chave antiga.");
                }

                var config = new AmazonS3Config
                {
                    ServiceURL = s3Options.ServiceURL,
                    ForcePathStyle = s3Options.ForcePathStyle,
                    AuthenticationRegion = s3Options.AuthenticationRegion
                };
                return new AmazonS3Client(new BasicAWSCredentials(s3Options.AccessKey, s3Options.SecretKey), config);
            });

            // Configure WhatsApp Options
            service.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));

            service.AddHttpClient<IDocumentSignatureService, ZapSignDocumentSignatureService>((serviceProvider, httpClient) =>
            {
                httpClient.BaseAddress = new Uri(configuration.GetSection("DocumentSigning")["BaseUrl"]!);
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, "Bearer " + configuration.GetSection("DocumentSigning")["AccessToken"]!);
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
                httpClient.BaseAddress = new Uri(configuration.GetSection("DocumentSigning")["BaseUrl"]!);
                httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, "Bearer " + configuration.GetSection("DocumentSigning")["AccessToken"]!);
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


            service.AddHttpClient<ISigningServiceAccountTokenProvider, SigningServiceAccountTokenProvider>((serviceProvider, httpClient) =>
            {
                httpClient.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<SigningServiceAccountTokenProvider>>();
                var context = new Polly.Context { ["Logger"] = logger };

                return HttpPolicyFactory.GetCombinedPolicy(retryCount: 3, timeoutSeconds: 30);
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            service.AddHttpClient<IWhatsAppService, WhatsAppService>((serviceProvider, httpClient) =>
            {
                var messagingOptions = configuration.GetSection(MessagingOptions.SectionName);
                httpClient.BaseAddress = new Uri(messagingOptions["BaseUrl"]!);
                httpClient.DefaultRequestHeaders.Add("apiKey", messagingOptions["ApiKey"]!);
                httpClient.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler((serviceProvider, request) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<WhatsAppService>>();
                var context = new Polly.Context { ["Logger"] = logger };

                return HttpPolicyFactory.GetCombinedPolicy(retryCount: 3, timeoutSeconds: 30);
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

            return service;
        }
    }
}
