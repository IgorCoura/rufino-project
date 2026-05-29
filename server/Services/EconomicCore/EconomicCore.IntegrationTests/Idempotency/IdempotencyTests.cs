namespace EconomicCore.IntegrationTests.Idempotency;

using System.Linq;
using System.Net;
using System.Net.Http.Json;
using EconomicCore.IntegrationTests.Contracts;
using EconomicCore.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

[Collection(nameof(IntegrationTestCollection))]
public sealed class IdempotencyTests : BaseIntegrationTest
{
    public IdempotencyTests(IntegrationTestWebAppFactory factory) : base(factory) { }

    private const string ResourceName = "Apartamento Idempotente";
    private static readonly Guid FixedRequestId = new("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    // Dois POST com o MESMO x-requestid criam o recurso uma única vez: a segunda chamada é reconhecida
    // como duplicata pelo IdentifiedCommandHandler e devolve resposta neutra (Id vazio), sem segundo
    // insert. Prova a idempotência fim-a-fim sobre a tabela client_requests.
    [Fact]
    public async Task PostResource_WhenSameRequestIdPostedTwice_ShouldPersistOnce()
    {
        SetRequestId(FixedRequestId);
        var payload = new CreateResourceRequest(ResourceName, "SERVICE");

        var first = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/resources", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstBody = await first.Content.ReadFromJsonAsync<ResourceResponse>();
        Assert.NotEqual(Guid.Empty, firstBody!.Id);

        var second = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/resources", payload);
        var secondBody = await second.Content.ReadFromJsonAsync<ResourceResponse>();
        Assert.Equal(Guid.Empty, secondBody!.Id);

        var resourceCount = await ExecuteDbContextAsync(db =>
            db.EconomicResources.AsNoTracking().CountAsync(r => r.Name == ResourceName));
        Assert.Equal(1, resourceCount);

        var requestCount = await ExecuteDbContextAsync(db =>
            db.ClientRequests.AsNoTracking().CountAsync(r => r.Id == FixedRequestId));
        Assert.Equal(1, requestCount);
    }

    // A mesma intenção enviada com x-requestid distintos cria dois recursos — requests diferentes
    // nunca colidem na tabela de idempotência.
    [Fact]
    public async Task PostResource_WhenDifferentRequestIds_ShouldPersistTwice()
    {
        var payload = new CreateResourceRequest(ResourceName, "SERVICE");

        SetRequestId(new("11111111-1111-1111-1111-111111111111"));
        var first = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/resources", payload);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        SetRequestId(new("22222222-2222-2222-2222-222222222222"));
        var second = await Client.PostAsJsonAsync($"/api/v1/{KnownIds.TenantA}/resources", payload);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondBody = await second.Content.ReadFromJsonAsync<ResourceResponse>();
        Assert.NotEqual(Guid.Empty, secondBody!.Id);

        var resourceCount = await ExecuteDbContextAsync(db =>
            db.EconomicResources.AsNoTracking().CountAsync(r => r.Name == ResourceName));
        Assert.Equal(2, resourceCount);
    }

    // Regressão (B3): N requests concorrentes com o MESMO x-requestid persistem o recurso uma única vez,
    // exatamente uma resposta traz Id real (as demais são neutras) e nenhuma responde 500 — a corrida na
    // PK de client_requests é absorvida pelo IdentifiedCommandHandler (DbUpdateException → confirma
    // duplicata → resposta neutra), em vez de vazar como erro de servidor.
    [Fact]
    public async Task PostResource_WhenSameRequestIdRacedConcurrently_ShouldPersistOnceAndNeverReturn500()
    {
        var requestId = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff");
        const string racedName = "Apartamento Concorrente";
        const int concurrency = 8;

        var tasks = Enumerable.Range(0, concurrency).Select(async _ =>
        {
            // Cada request com seu próprio HttpClient: evita corrida em DefaultRequestHeaders.
            using var client = Factory.CreateClient();
            client.DefaultRequestHeaders.Add("x-requestid", requestId.ToString());
            return await client.PostAsJsonAsync(
                $"/api/v1/{KnownIds.TenantA}/resources",
                new CreateResourceRequest(racedName, "SERVICE"));
        }).ToList();

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True((int)r.StatusCode < 500, $"status inesperado: {(int)r.StatusCode}"));

        var bodies = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<ResourceResponse>()));
        Assert.Equal(1, bodies.Count(b => b!.Id != Guid.Empty));

        var resourceCount = await ExecuteDbContextAsync(db =>
            db.EconomicResources.AsNoTracking().CountAsync(r => r.Name == racedName));
        Assert.Equal(1, resourceCount);

        var requestCount = await ExecuteDbContextAsync(db =>
            db.ClientRequests.AsNoTracking().CountAsync(r => r.Id == requestId));
        Assert.Equal(1, requestCount);
    }

    private void SetRequestId(Guid requestId)
    {
        Client.DefaultRequestHeaders.Remove("x-requestid");
        Client.DefaultRequestHeaders.Add("x-requestid", requestId.ToString());
    }
}
