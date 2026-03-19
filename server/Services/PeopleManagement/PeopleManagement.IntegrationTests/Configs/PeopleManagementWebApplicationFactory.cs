using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Testcontainers.LocalStack;
using DotNet.Testcontainers.Containers;
using EntityFramework.Exceptions.PostgreSQL;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using PeopleManagement.API.Authorization;
using PeopleManagement.Infra.Context;
using Testcontainers.PostgreSql;

namespace PeopleManagement.IntegrationTests.Configs
{

    public class PeopleManagementWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15.1-alpine")
            .WithDatabase("PeopleManagementTestDb")
            .WithUsername("admin")
            .WithPassword("admin")
            .Build();

        private readonly PostgreSqlContainer _hangfireDbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15.1-alpine")
            .WithDatabase("HangfireTestDb")
            .WithUsername("admin")
            .WithPassword("admin")
            .Build();

        private readonly LocalStackContainer _localStackContainer = new LocalStackBuilder()
           .Build();

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();
            await _hangfireDbContainer.StartAsync();
            await _localStackContainer.StartAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {

            builder.ConfigureTestServices(services =>
            {
                //Config DataBase
                var dbContext = services.SingleOrDefault(
                        d => d.ServiceType ==
                            typeof(IDbContextFactory<PeopleManagementContext>));
                if (dbContext != null)
                    services.Remove(dbContext);

                services.AddDbContextFactory<PeopleManagementContext>(options =>
                {
                    options.UseNpgsql(
                        _dbContainer.GetConnectionString(),
                        npgsqlOptionsAction: sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
                        }).UseExceptionProcessor()
                        .EnableDetailedErrors()
                        .EnableSensitiveDataLogging();
                });


                //Config Hangfire

                var hangfireServerDescriptor = services.SingleOrDefault(
               d => d.ServiceType == typeof(IHostedService) &&
                    d.ImplementationType?.Name.Contains("HangfireServer") == true);

                if (hangfireServerDescriptor != null)
                {
                    services.Remove(hangfireServerDescriptor);
                }
                services.AddHangfire(configuration =>
                configuration.UsePostgreSqlStorage(options =>
                {
                    options.UseNpgsqlConnection(_dbContainer.GetConnectionString());
                }));


                services.AddHangfireServer();

                //Config S3
                var amazonS3 = services.SingleOrDefault(d => d.ServiceType == typeof(IAmazonS3));
                if (amazonS3 != null)
                    services.Remove(amazonS3);

                services.AddSingleton<IAmazonS3>(_ =>
                {
                    var config = new AmazonS3Config
                    {
                        ServiceURL = _localStackContainer.GetConnectionString(),
                        ForcePathStyle = true
                    };
                    return new AmazonS3Client(new BasicAWSCredentials("test", "test"), config);
                });

                //Auth

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = MockAuthenticationHandler.AuthScheme;
                    options.DefaultChallengeScheme = MockAuthenticationHandler.AuthScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>(MockAuthenticationHandler.AuthScheme, options => { });

                services.AddSingleton<IAuthorizationHandler, MockAccessRequirementHandler>();

                services.AddSingleton<IAuthorizationPolicyProvider>(x => new ProtectedResourcePolicyProvider(
                param =>
                {
                    var policy = new AuthorizationPolicyBuilder(MockAuthenticationHandler.AuthScheme);
                    policy.AddRequirements(new MockAccessRequirement("company", "companies"));
                    return policy;
                })
            ); 
            });
        }

        public PeopleManagementContext GetContext()
        {
            var scope = this.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await _dbContainer.StopAsync();
            await _localStackContainer.StopAsync();
            await _hangfireDbContainer.StopAsync();
        }
    }

}
