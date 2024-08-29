using Azure.Storage.Blobs;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Infra.Context;
using Testcontainers.Azurite;
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

        private readonly AzuriteContainer _azuriteContainer = new AzuriteBuilder()
           .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
           .Build();

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();
            await _azuriteContainer.StartAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {

            builder.ConfigureTestServices(services =>
            {
                //Config DataBase
                var dbContext = services.SingleOrDefault(
                        d => d.ServiceType ==
                            typeof(DbContextOptions<PeopleManagementContext>));
                if (dbContext != null)
                    services.Remove(dbContext);

                services.AddDbContext<PeopleManagementContext>(options =>
                {
                    options.UseNpgsql(
                        _dbContainer.GetConnectionString(),
                        npgsqlOptionsAction: sqlOptions =>
                        {
                            sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorCodesToAdd: null);
                        }).UseExceptionProcessor();
                });

                //Config AzureBlob
                var azureBlob = services.SingleOrDefault(d => d.ServiceType == typeof(BlobServiceClient));
                if(azureBlob != null)
                    services.Remove(azureBlob);

                services.AddSingleton(_ => new BlobServiceClient(_azuriteContainer.GetConnectionString()));
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
            await _azuriteContainer.StopAsync();
        }
    }

}
