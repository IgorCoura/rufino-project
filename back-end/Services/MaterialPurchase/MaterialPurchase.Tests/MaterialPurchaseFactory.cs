using Commom.Tests;
using MaterialPurchase.Infra.Context;
using MaterialPurchase.Tests.Utils;

namespace MaterialPurchase.Tests
{
    public class MaterialPurchaseFactory : CustomWebApplicationFactory<MaterialPurchaseContext, Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {               
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
