using EntityFramework.Exceptions.Sqlite;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Infra.Context;

namespace PeopleManagement.Tests.Configs
{

    public class PeopleManagementWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            builder.ConfigureServices(services =>
            {

                //var publishSubscribe = services.SingleOrDefault(
                //    d => d.ServiceType ==
                //        typeof(IPublishSubscribe));

                //if (publishSubscribe != null)
                //    services.Remove(publishSubscribe);

                //services.AddScoped<IPublishSubscribe, PublishSubscribeMock>();


                //services.AddOptions<AuthorizationIdOptions>()
                //.Configure(x => x.Schema = "Local");

                //services.AddAuthentication(options =>
                //{
                //    options.DefaultAuthenticateScheme = LocalAuthenticationHandler.AuthScheme;
                //    options.DefaultChallengeScheme = LocalAuthenticationHandler.AuthScheme;
                //})
                //.AddScheme<AuthenticationSchemeOptions, LocalAuthenticationHandler>(LocalAuthenticationHandler.AuthScheme, options => { });

                //Config DataBase
                var dbContext = services.SingleOrDefault(
                    d => d.ServiceType ==
                        typeof(DbContextOptions<PeopleManagementContext>));

                if (dbContext != null)
                    services.Remove(dbContext);

                services.AddDbContext<PeopleManagementContext>(options =>
                {
                    options.UseSqlite(connection)
                    .UseExceptionProcessor()
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
                });

                var sp = services.BuildServiceProvider();

                using var scope = sp.CreateScope();
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<PeopleManagementContext>();
                db.Database.EnsureCreated();

            });
        }

        public PeopleManagementContext GetContext()
        {
            var scope = this.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<PeopleManagementContext>();
        }

    }

}
