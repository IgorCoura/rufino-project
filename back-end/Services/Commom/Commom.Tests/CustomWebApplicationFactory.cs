using Commom.API.AuthorizationIds;
using Commom.Infra.Base;
using EntityFramework.Exceptions.Sqlite;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Commom.Tests
{
    public class CustomWebApplicationFactory<Context, TProgram> : WebApplicationFactory<TProgram> where Context : BaseContext where TProgram : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            builder.ConfigureServices(services =>
            {

                var dbContext = services.SingleOrDefault(
                    d => d.ServiceType ==
                        typeof(DbContextOptions<Context>));
                services.Remove(dbContext);

                services.AddOptions<AuthorizationIdOptions>()
                .Configure(x => x.Schema = "Local");

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = LocalAuthenticationHandler.AuthScheme;
                    options.DefaultChallengeScheme = LocalAuthenticationHandler.AuthScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, LocalAuthenticationHandler>(LocalAuthenticationHandler.AuthScheme, options => { });

                services.AddDbContext<Context>(options =>
                {
                    options.UseSqlite(connection)
                    .UseExceptionProcessor()
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
                });
               
            });
        }
    }
}
