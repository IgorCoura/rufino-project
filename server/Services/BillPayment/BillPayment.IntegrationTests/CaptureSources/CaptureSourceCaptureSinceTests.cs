namespace BillPayment.IntegrationTests.CaptureSources;

using System.Net;
using System.Net.Http.Json;
using BillPayment.IntegrationTests.Contracts;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// O piso temporal da captura, pela borda HTTP.
/// </summary>
/// <remarks>
/// A regra que estes testes existem para guardar não é o campo — é o <strong>descarte dos
/// cursores</strong>. A delta query do Graph grava as opções de consulta dentro do
/// <c>deltaLink</c> que devolve, então uma data nova sobre um cursor velho seria decorativa: a
/// varredura seguinte continuaria filtrando pela data antiga, sem erro e sem log.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class CaptureSourceCaptureSinceTests : BaseIntegrationTest
{
    private static readonly Guid TenantA = new("0195a1f0-0000-7000-8000-000000000001");
    private static readonly Guid TenantB = new("0195a1f0-0000-7000-8000-000000000002");

    private readonly HttpClient _reachable;
    private readonly FakeMailboxReader _mailbox;

    public CaptureSourceCaptureSinceTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        var host = factory.WithReachableMailbox();
        _reachable = host.CreateClient().Authenticated();
        _mailbox = host.Services.GetRequiredService<FakeMailboxReader>();
    }

    private static Uri SourcesFor(Guid tenantId) => new($"/api/v1/{tenantId}/capture-sources", UriKind.Relative);

    private static Uri SinceFor(Guid tenantId, Guid id)
        => new($"/api/v1/{tenantId}/capture-sources/{id}/capture-since", UriKind.Relative);

    private static Uri SourceFor(Guid tenantId, Guid id)
        => new($"/api/v1/{tenantId}/capture-sources/{id}", UriKind.Relative);

    private static Uri SyncFor(Guid tenantId, Guid id)
        => new($"/api/v1/{tenantId}/capture-sources/{id}/sync", UriKind.Relative);

    private async Task<Guid> ConnectAsync(Guid tenantId, string address, DateOnly? captureSince = null)
    {
        var response = await _reachable.PostAsJsonAsync(
            SourcesFor(tenantId),
            new ConnectCaptureSourceRequest(
                "MicrosoftGraphMailbox",
                "Caixa de contas a pagar",
                address,
                "segredo-do-registro-de-app",
                CaptureSince: captureSince));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ConnectCaptureSourceResponseDto>();
        return body!.Id;
    }

    private async Task<CaptureSourceResponseDto> GetAsync(Guid tenantId, Guid id)
    {
        var response = await _reachable.GetAsync(SourceFor(tenantId, id));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CaptureSourceResponseDto>())!;
    }

    // Conectar informando a data guarda o piso, e ele volta no contrato de leitura.
    [Fact]
    public async Task PostCaptureSource_WithCaptureSince_ShouldPersistAndExposeTheFloor()
    {
        var since = new DateOnly(2026, 5, 27);

        var id = await ConnectAsync(TenantA, "piso-na-conexao@empresa.com.br", since);

        Assert.Equal(since, (await GetAsync(TenantA, id)).CaptureSince);
    }

    // Conectar sem data mantém o comportamento de sempre: a caixa inteira.
    [Fact]
    public async Task PostCaptureSource_WithoutCaptureSince_ShouldHaveNoFloor()
    {
        var id = await ConnectAsync(TenantA, "sem-piso@empresa.com.br");

        Assert.Null((await GetAsync(TenantA, id)).CaptureSince);
    }

    // TESTE-ÂNCORA: trocar o piso zera o cursor de TODAS as pastas. Sem isso o provedor
    // continuaria mandando pela data velha gravada dentro do deltaLink.
    [Fact]
    public async Task PutCaptureSince_ShouldDropEveryFolderCursor()
    {
        var id = await ConnectAsync(TenantA, "troca-de-piso@empresa.com.br");

        // Sincroniza para a pasta ganhar cursor.
        (await _reachable.PostAsync(SyncFor(TenantA, id), null)).EnsureSuccessStatusCode();
        Assert.All((await GetAsync(TenantA, id)).Folders, f => Assert.True(f.HasSyncCursor));

        var response = await _reachable.PutAsJsonAsync(
            SinceFor(TenantA, id), new ChangeCaptureSourceSinceRequest(new DateOnly(2026, 5, 27)));

        response.EnsureSuccessStatusCode();

        var source = await GetAsync(TenantA, id);
        Assert.Equal(new DateOnly(2026, 5, 27), source.CaptureSince);
        Assert.All(source.Folders, f => Assert.False(f.HasSyncCursor));
    }

    // Limpar o piso devolve a fonte à caixa inteira, e também descarta o cursor.
    [Fact]
    public async Task PutCaptureSince_WithNull_ShouldClearTheFloorAndDropTheCursor()
    {
        var id = await ConnectAsync(TenantA, "limpa-piso@empresa.com.br", new DateOnly(2026, 5, 27));
        (await _reachable.PostAsync(SyncFor(TenantA, id), null)).EnsureSuccessStatusCode();

        var response = await _reachable.PutAsJsonAsync(
            SinceFor(TenantA, id), new ChangeCaptureSourceSinceRequest(null));

        response.EnsureSuccessStatusCode();

        var source = await GetAsync(TenantA, id);
        Assert.Null(source.CaptureSince);
        Assert.All(source.Folders, f => Assert.False(f.HasSyncCursor));
    }

    // Piso no futuro é recusado com 400 e BLP.CPS20 — fonte que não captura nada e não avisa.
    [Fact]
    public async Task PutCaptureSince_WithFutureDate_ShouldReturnBadRequestWith_BLP_CPS20()
    {
        var id = await ConnectAsync(TenantA, "piso-futuro@empresa.com.br");
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        var response = await _reachable.PutAsJsonAsync(
            SinceFor(TenantA, id), new ChangeCaptureSourceSinceRequest(future));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("BLP.CPS20", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    // Fonte de outro tenant é inalcançável: 404, e não 403, porque o filtro é do repositório.
    [Fact]
    public async Task PutCaptureSince_OnAnotherTenantsSource_ShouldReturnNotFound()
    {
        var id = await ConnectAsync(TenantA, "de-outro-tenant@empresa.com.br");

        var response = await _reachable.PutAsJsonAsync(
            SinceFor(TenantB, id), new ChangeCaptureSourceSinceRequest(new DateOnly(2026, 5, 27)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // O piso chega ao leitor de caixa na varredura — é o que faz o corte existir de verdade.
    [Fact]
    public async Task Sync_ShouldHandTheFloorToTheMailboxReader()
    {
        var since = new DateOnly(2026, 5, 27);
        var id = await ConnectAsync(TenantA, "piso-na-varredura@empresa.com.br", since);

        (await _reachable.PostAsync(SyncFor(TenantA, id), null)).EnsureSuccessStatusCode();

        Assert.Equal(since, _mailbox.LastCapturedSince);
    }
}
