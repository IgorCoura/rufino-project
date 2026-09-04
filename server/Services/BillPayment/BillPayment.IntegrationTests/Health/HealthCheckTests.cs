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

    // Toda resposta sai com os cabeçalhos de segurança: nosniff é o que impede o navegador de
    // "adivinhar" HTML num anexo servido com o tipo que o remetente declarou.
    [Fact]
    public async Task AnyResponse_ShouldCarryTheSecurityHeaders()
    {
        var response = await Client.GetAsync(new Uri("/api/health", UriKind.Relative));

        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
    }
}
