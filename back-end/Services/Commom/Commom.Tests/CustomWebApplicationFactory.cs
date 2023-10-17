using Commom.Auth.Authorization;
using Commom.Infra.Base;
using Commom.MessageBroker.Bus;
using Commom.Tests.Mocks;
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
                
                if(dbContext != null)
                    services.Remove(dbContext);

                var publishSubscribe = services.SingleOrDefault(
                    d => d.ServiceType ==
                        typeof(IPublishSubscribe));

                if(publishSubscribe != null)
                    services.Remove(publishSubscribe);

                services.AddScoped<IPublishSubscribe, PublishSubscribeMock>();


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
