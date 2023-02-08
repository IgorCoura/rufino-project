using Commom.Tests;
using Commom.Tests.Utils;
using MaterialControl.Infra.Context;

namespace MaterialControl.Tests
{
    public class MaterialControlFactory : CustomWebApplicationFactory<MaterialControlContext, Program>
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
                    var db = scopedServices.GetRequiredService<MaterialControlContext>();
                    var logger = scopedServices
                        .GetRequiredService<ILogger<MaterialControlFactory>>();

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
