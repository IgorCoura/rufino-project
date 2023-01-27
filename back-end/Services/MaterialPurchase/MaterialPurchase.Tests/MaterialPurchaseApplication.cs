using MaterialPurchase.Infra.Context;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MaterialPurchase.Tests
{
    public class MaterialPurchaseApplication : WebApplicationFactory<Program>
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
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType ==
                        typeof(DbContextOptions<MaterialPurchaseContext>));

                services.Remove(descriptor);

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
                        .GetRequiredService<ILogger<MaterialPurchaseApplication>>();

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
