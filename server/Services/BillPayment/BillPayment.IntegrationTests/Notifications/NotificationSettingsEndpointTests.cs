namespace BillPayment.IntegrationTests.Notifications;

using System.Net;
using System.Net.Http.Json;
using BillPayment.IntegrationTests.Infrastructure;

/// <summary>
/// Os destinatários do aviso pela porta da frente — o controller não tinha teste nenhum.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class NotificationSettingsEndpointTests : BaseIntegrationTest
{
    private static readonly Guid Tenant = TestTenants.Primary;
    private static readonly Guid OtherTenant = TestTenants.Secondary;

    public NotificationSettingsEndpointTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private static Uri RouteFor(Guid tenantId) => new($"/api/v1/{tenantId}/notification-settings", UriKind.Relative);

    // Tenant que nunca configurou nada lê o estado desligado, sem destinatário — nunca 404.
    [Fact]
    public async Task Get_WithoutAnyConfiguration_ShouldReturnTheDisabledDefault()
    {
        var settings = await Client.GetFromJsonAsync<Dto>(RouteFor(Tenant));

        Assert.NotNull(settings);
        Assert.False(settings!.IsEnabled);
        Assert.Empty(settings.Recipients);
    }

    // Configurar cria na primeira chamada e substitui a lista inteira nas seguintes.
    [Fact]
    public async Task Put_ShouldCreateThenReplaceTheRecipients()
    {
        var first = await Client.PutAsJsonAsync(RouteFor(Tenant), new Request(["financeiro@empresa.com.br"], true));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await Client.PutAsJsonAsync(RouteFor(Tenant), new Request(["contas@empresa.com.br"], true));
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var settings = await Client.GetFromJsonAsync<Dto>(RouteFor(Tenant));
        Assert.True(settings!.IsEnabled);
        Assert.Equal(["contas@empresa.com.br"], settings.Recipients);
    }

    // Ligar sem destinatário é recusado — BLP.NTF03, conflito de estado (409): um canal ligado
    // que não avisa ninguém é o modo de falha silencioso que o ADR-014 combate.
    [Fact]
    public async Task Put_EnabledWithoutRecipients_ShouldReturnConflict()
    {
        var response = await Client.PutAsJsonAsync(RouteFor(Tenant), new Request([], true));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("BLP.NTF03", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    // Endereço inválido é recusado — BLP.NTF01.
    [Fact]
    public async Task Put_WithAnInvalidRecipient_ShouldReturnBadRequest()
    {
        var response = await Client.PutAsJsonAsync(RouteFor(Tenant), new Request(["nao-e-email"], false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("BLP.NTF01", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    // Os destinatários de um tenant não aparecem para o outro.
    [Fact]
    public async Task Get_FromAnotherTenant_ShouldNotSeeTheRecipients()
    {
        await Client.PutAsJsonAsync(RouteFor(Tenant), new Request(["financeiro@empresa.com.br"], true));

        var other = await Client.GetFromJsonAsync<Dto>(RouteFor(OtherTenant));

        Assert.False(other!.IsEnabled);
        Assert.Empty(other.Recipients);
    }

    private sealed record Request(IReadOnlyCollection<string> Recipients, bool IsEnabled);

    private sealed record Dto(IReadOnlyCollection<string> Recipients, bool IsEnabled);
}
