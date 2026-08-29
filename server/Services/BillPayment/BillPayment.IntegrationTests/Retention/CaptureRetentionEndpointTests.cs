namespace BillPayment.IntegrationTests.Retention;

using System.Net;
using System.Net.Http.Json;
using BillPayment.IntegrationTests.Infrastructure;

/// <summary>
/// A janela de retenção do livro-caixa pela porta da frente — o controller não tinha teste nenhum.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CaptureRetentionEndpointTests : BaseIntegrationTest
{
    private static readonly Guid Tenant = TestTenants.Primary;
    private static readonly Guid OtherTenant = TestTenants.Secondary;

    public CaptureRetentionEndpointTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private static Uri RouteFor(Guid tenantId) => new($"/api/v1/{tenantId}/capture-retention", UriKind.Relative);

    // A política nasce desligada, com a janela padrão e a lista de janelas possíveis para a tela.
    [Fact]
    public async Task Get_WithoutAnyConfiguration_ShouldReturnTheDisabledDefault()
    {
        var policy = await Client.GetFromJsonAsync<Dto>(RouteFor(Tenant));

        Assert.NotNull(policy);
        Assert.False(policy!.IsEnabled);
        Assert.Equal(90, policy.WindowDays);
        Assert.Contains(30, policy.AvailableWindowDays);
    }

    // Configurar pela API liga e troca a janela; a leitura reflete.
    [Fact]
    public async Task Put_ShouldEnableAndPersistTheWindow()
    {
        var response = await Client.PutAsJsonAsync(RouteFor(Tenant), new Request(true, 30));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var policy = await Client.GetFromJsonAsync<Dto>(RouteFor(Tenant));
        Assert.True(policy!.IsEnabled);
        Assert.Equal(30, policy.WindowDays);
    }

    // A janela é de faixa fechada: prazo fora do catálogo é recusado pelo domínio, não aceito
    // como número qualquer.
    [Fact]
    public async Task Put_WithAWindowOutsideTheCatalogue_ShouldReturnBadRequest()
    {
        var response = await Client.PutAsJsonAsync(RouteFor(Tenant), new Request(true, 15));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // A política de um tenant não vaza para o outro.
    [Fact]
    public async Task Get_FromAnotherTenant_ShouldReturnItsOwnDefault()
    {
        await Client.PutAsJsonAsync(RouteFor(Tenant), new Request(true, 7));

        var other = await Client.GetFromJsonAsync<Dto>(RouteFor(OtherTenant));

        Assert.False(other!.IsEnabled);
        Assert.Equal(90, other.WindowDays);
    }

    private sealed record Request(bool IsEnabled, int WindowDays);

    private sealed record Dto(bool IsEnabled, int WindowDays, IReadOnlyList<int> AvailableWindowDays);
}
