namespace BillPayment.IntegrationTests.Authorization;

using System.Net;
using BillPayment.API.Authorization;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O guard anti-IDOR pela borda HTTP: sem token não entra, e com token de outro tenant não passa.
/// </summary>
/// <remarks>
/// Existe porque o resto da suíte declara os tenants que alcança e, portanto, sempre passa pelo
/// guard. Sem esta classe, o caminho da NEGATIVA nunca seria exercitado — e um guard que só é
/// testado quando concede não está testado.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class RouteGuardTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private static readonly Guid Foreign = new("0195a1f0-0000-7000-8000-0000000000ff");

    private static Uri BillsRouteFor(Guid tenantId) => new($"/api/v1/{tenantId}/bills", UriKind.Relative);

    // Requisição sem nenhum header de autenticação é recusada com 401, nunca com 200 nem 403.
    [Fact]
    public async Task GetBills_WithoutToken_ShouldReturnUnauthorized()
    {
        var anonymous = Factory.CreateClient();

        var response = await anonymous.GetAsync(BillsRouteFor(TestTenants.Primary));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Token válido, mas o tenant da rota não está no claim: 403, e o dado do outro tenant não sai.
    [Fact]
    public async Task GetBills_WhenRouteTenantIsNotInTheClaim_ShouldReturnForbidden()
    {
        var response = await Client.GetAsync(BillsRouteFor(Foreign));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // Tenant da rota presente no claim: a requisição atravessa o guard e chega ao handler.
    [Fact]
    public async Task GetBills_WhenRouteTenantIsInTheClaim_ShouldReturnOk()
    {
        var response = await Client.GetAsync(BillsRouteFor(TestTenants.Primary));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // O guard compara Guid, e Guid não tem caixa: a mesma rota em MAIÚSCULAS continua concedida.
    // Regressão da comparação ordinal herdada do PeopleManagement, que devolvia 403 sem explicação
    // quando o cliente escrevia o Guid na URL de um jeito e o provisionador gravava o claim de outro.
    [Fact]
    public async Task GetBills_WhenRouteTenantDiffersOnlyInCasing_ShouldReturnOk()
    {
        var upperCased = TestTenants.Primary.ToString().ToUpperInvariant();

        var response = await Client.GetAsync(new Uri($"/api/v1/{upperCased}/bills", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // O claim que a suíte manda é o mesmo que o host lê. Sem esta afirmação, trocar o claim no
    // appsettings deixaria os 365 testes verdes exercitando um guard que a produção não monta —
    // e o BillPayment lê o claim do PRODUTO (bp_tenants), não o `tenants` genérico, porque o
    // genérico daria acesso a quem só assinou o outro produto da plataforma.
    [Fact]
    public void ConfiguredClaim_ShouldMatchTheHeaderTheSuiteSends()
    {
        var options = Factory.Services.GetRequiredService<AuthorizationOptions>();

        Assert.Equal(MockAuthenticationHandler.TenantsHeader, options.RouteClaimTypeRequirement);
        Assert.Equal("tenantId", options.RouteNameRequirement);
    }

    // A sonda de vida responde sem token — é [AllowAnonymous] de propósito, para continuar
    // respondendo justamente quando o servidor de autorização cai.
    [Fact]
    public async Task GetHealth_WithoutToken_ShouldReturnOk()
    {
        var anonymous = Factory.CreateClient();

        var response = await anonymous.GetAsync(new Uri("/api/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
