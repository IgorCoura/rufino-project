using Commom.Tests;
using CustSuprManager.Infra.Context;
using CustSuprManager.Tests.Utils;

namespace CustSuprManager.Tests
{
    public class CustSuprManagerFactory : CustomWebApplicationFactory<CustSuprManagerContext, Program>
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
                    var db = scopedServices.GetRequiredService<CustSuprManagerContext>();
                    var logger = scopedServices
                        .GetRequiredService<ILogger<CustSuprManagerContext>>();

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
