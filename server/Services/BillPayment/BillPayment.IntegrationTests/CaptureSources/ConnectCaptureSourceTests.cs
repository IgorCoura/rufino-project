namespace BillPayment.IntegrationTests.CaptureSources;

using System.Net;
using System.Net.Http.Json;
using BillPayment.Domain.CaptureSources;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

[Collection(nameof(IntegrationTestCollection))]
public sealed class ConnectCaptureSourceTests : BaseIntegrationTest
{
    private static readonly Guid TenantA = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid TenantB = new("0195a1f0-0000-7000-8000-000000000002");

    private const string SharedMailbox = "contas@empresa.com.br";

    private readonly HttpClient _reachable;

    public ConnectCaptureSourceTests(IntegrationTestWebAppFactory factory) : base(factory)
        => _reachable = factory.WithReachableMailbox().CreateClient().Authenticated();

    private static Uri RouteFor(Guid tenantId) => new($"/api/v1/{tenantId}/capture-sources", UriKind.Relative);

    private static ConnectCaptureSourceRequest Payload(string address = SharedMailbox)
        => new("MicrosoftGraphMailbox", "Caixa de contas a pagar", address, "segredo-do-registro-de-app");

    // O Id é value-converted: o EF não traduz comparação sobre .Value, só sobre o tipo forte.
    private Task<CaptureSource> LoadAsync(Guid id)
    {
        var sourceId = CaptureSourceId.From(id);
        return ExecuteDbContextAsync(db => db.CaptureSources.AsNoTracking().FirstAsync(s => s.Id == sourceId));
    }

    // Conectar com acesso provado grava a fonte e devolve o id.
    [Fact]
    public async Task PostCaptureSource_WithReachableMailbox_ShouldPersistSource()
    {
        var response = await _reachable.PostAsJsonAsync(RouteFor(TenantA), Payload());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConnectCaptureSourceResponseDto>();

        var stored = await LoadAsync(body!.Id);

        Assert.Equal(SharedMailbox, stored.Address);
        Assert.True(stored.IsEnabled);
        Assert.NotNull(stored.Credential);
    }

    // A credencial vai para o cofre cifrada, e a fonte guarda só o ponteiro — nunca o segredo.
    [Fact]
    public async Task PostCaptureSource_ShouldStoreSecretInVaultAndKeepOnlyThePointer()
    {
        var response = await _reachable.PostAsJsonAsync(RouteFor(TenantA), Payload());
        var body = await response.Content.ReadFromJsonAsync<ConnectCaptureSourceResponseDto>();

        var stored = await LoadAsync(body!.Id);

        var segredos = await ExecuteDbContextAsync(db => db.TenantSecrets.AsNoTracking().CountAsync());

        Assert.Equal(1, segredos);
        Assert.StartsWith("bpv1:", stored.Credential!.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("segredo-do-registro-de-app", stored.Credential.ToString(), StringComparison.Ordinal);
    }

    // A segunda conta a conectar a MESMA caixa conecta normalmente e a resposta é IDÊNTICA à da
    // primeira: nem booleano de "já monitorada", nem id, nem nome do outro. Cada tenant relê a
    // caixa sozinho, sem saber que não está sozinho (decisão de 2026-08-28 sobre o ADR-008).
    [Fact]
    public async Task PostCaptureSource_WhenAnotherAccountAlreadyMonitors_ShouldConnectWithoutRevealingIt()
    {
        await _reachable.PostAsJsonAsync(RouteFor(TenantB), Payload());

        var response = await _reachable.PostAsJsonAsync(RouteFor(TenantA), Payload());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConnectCaptureSourceResponseDto>();
        Assert.NotEqual(Guid.Empty, body!.Id);

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(TenantB.ToString(), raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("monitored", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("shared", raw, StringComparison.OrdinalIgnoreCase);
    }

    // A mesma conta não conecta a mesma caixa duas vezes — BLP.CPS10, 409.
    [Fact]
    public async Task PostCaptureSource_TwiceInSameTenant_ShouldReturnConflict()
    {
        await _reachable.PostAsJsonAsync(RouteFor(TenantA), Payload());

        var response = await _reachable.PostAsJsonAsync(RouteFor(TenantA), Payload());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // Sem adapter de caixa, conectar FALHA e não deixa fonte nem credencial órfã no cofre — a
    // prova de acesso do ADR-006 é pré-condição, não formalidade.
    [Fact]
    public async Task PostCaptureSource_WithUnreachableMailbox_ShouldFailAndLeaveNothingBehind()
    {
        var response = await Client.PostAsJsonAsync(RouteFor(TenantA), Payload());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var fontes = await ExecuteDbContextAsync(db => db.CaptureSources.AsNoTracking().CountAsync());
        var segredos = await ExecuteDbContextAsync(db => db.TenantSecrets.AsNoTracking().CountAsync());

        Assert.Equal(0, fontes);
        Assert.Equal(0, segredos);
    }

    // Endereço que não é e-mail válido é recusado antes de qualquer chamada ao provedor.
    [Fact]
    public async Task PostCaptureSource_WithInvalidMailbox_ShouldReturnBadRequest()
    {
        var response = await _reachable.PostAsJsonAsync(RouteFor(TenantA), Payload("nao-e-email"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // A leitura da fonte nunca devolve credencial nem o ponteiro do cofre (ADR-009).
    [Fact]
    public async Task GetCaptureSource_ShouldNeverExposeCredential()
    {
        var created = await _reachable.PostAsJsonAsync(RouteFor(TenantA), Payload());
        var body = await created.Content.ReadFromJsonAsync<ConnectCaptureSourceResponseDto>();

        var response = await _reachable.GetAsync(new Uri($"{RouteFor(TenantA)}/{body!.Id}", UriKind.Relative));
        var raw = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("segredo-do-registro-de-app", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("bpv1:", raw, StringComparison.Ordinal);
        Assert.Contains("\"hasCredential\":true", raw, StringComparison.OrdinalIgnoreCase);
    }

    // A fonte de um tenant não é visível pelo outro — isolamento sem exceção.
    [Fact]
    public async Task GetCaptureSource_FromAnotherTenant_ShouldReturnNotFound()
    {
        var created = await _reachable.PostAsJsonAsync(RouteFor(TenantB), Payload());
        var body = await created.Content.ReadFromJsonAsync<ConnectCaptureSourceResponseDto>();

        var response = await _reachable.GetAsync(new Uri($"{RouteFor(TenantA)}/{body!.Id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
