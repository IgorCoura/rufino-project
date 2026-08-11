namespace BillPayment.IntegrationTests.Health;

using System.Net;
using BillPayment.IntegrationTests.Infrastructure;

[Collection(nameof(IntegrationTestCollection))]
public sealed class HealthCheckTests : BaseIntegrationTest
{
    public HealthCheckTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    // O esqueleto sobe ponta a ponta: API + DbContext + Postgres (Testcontainers) respondem 200 no /api/health.
    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await Client.GetAsync(new Uri("/api/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
