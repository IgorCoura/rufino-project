using Docker.DotNet.Models;
using EntityFramework.Exceptions.PostgreSQL;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
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

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

            var aa = _dbContainer.GetConnectionString();

            builder.ConfigureTestServices(services =>
            {
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
        }
    }

}
