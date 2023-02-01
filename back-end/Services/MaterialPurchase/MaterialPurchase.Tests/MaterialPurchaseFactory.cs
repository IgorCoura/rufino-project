using Commom.API.FunctionIdAuthorization;
using MaterialPurchase.Infra.Context;
using MaterialPurchase.Tests.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MaterialPurchase.Tests
{
    public class MaterialPurchaseFactory : WebApplicationFactory<Program>
    {
        public MaterialPurchaseContext GetContext()
        {
            var scope = this.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<MaterialPurchaseContext>();
        }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            builder.ConfigureServices(services =>
            {

                var dbContext = services.SingleOrDefault(
                    d => d.ServiceType ==
                        typeof(DbContextOptions<MaterialPurchaseContext>));
                services.Remove(dbContext);

                services.AddOptions<FunctionIdAuthorizationOptions>()
                .Configure(x => x.Schema = "Local");

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = LocalAuthenticationHandler.AuthScheme;
                    options.DefaultChallengeScheme = LocalAuthenticationHandler.AuthScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, LocalAuthenticationHandler>(LocalAuthenticationHandler.AuthScheme, options => { });

                services.AddDbContext<MaterialPurchaseContext>(options =>
                {
                    options.UseSqlite(connection)
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
                });

                var sp = services.BuildServiceProvider();

                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<MaterialPurchaseContext>();
                    var logger = scopedServices
                        .GetRequiredService<ILogger<MaterialPurchaseFactory>>();

                    db.Database.EnsureCreated();

                    try
                    {
                        db.InsertDataForTests();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred seeding the " +
                            "database with test messages. Error: {Message}", ex.Message);
                    }
                }
            });
        }
    }
}
