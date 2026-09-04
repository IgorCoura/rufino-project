namespace BillPayment.IntegrationTests.Authorization;

using BillPayment.API.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

/// <summary>
/// O cache do retrato de permissões: uma ida ao servidor de autorização por TOKEN, não por
/// requisição — e o que acontece quando ele cai.
/// </summary>
/// <remarks>
/// Sem containers e sem HTTP: o que se mede é a política de cache, e um teste que subisse o host
/// mediria o pipeline. A suíte de integração roda com o cache DESLIGADO (ver
/// <c>IntegrationTestWebAppFactory</c>), então esta classe é o único lugar que o exercita.
/// </remarks>
public sealed class RptCacheTests
{
    /// <summary>Conta quantas vezes o servidor de autorização foi realmente consultado.</summary>
    private sealed class CountingClient(params RptFetchResult[] answers) : IAuthorizationServerClient
    {
        public int CallCount { get; private set; }

        public Task<RptFetchResult> FetchAllPermissionsAsync(CancellationToken cancellationToken = default)
        {
            var answer = answers[Math.Min(CallCount, answers.Length - 1)];
            CallCount++;
            return Task.FromResult(answer);
        }
    }

    private static readonly string[] ApproveScopes = ["approve"];

    private static readonly string[] ViewScopes = ["view"];

    private static readonly RptFetchResult BillApprove =
        RptFetchResult.Resolved(RptSnapshot.From([("bill", ApproveScopes)]));

    private static (RptCache Cache, IHttpContextAccessor Accessor, MemoryCache Store) Build(
        IAuthorizationServerClient client, AuthorizationOptions? options = null)
    {
        var store = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var cache = new RptCache(
            accessor, client, store,
            options ?? new AuthorizationOptions(),
            NullLogger<RptCache>.Instance);

        return (cache, accessor, store);
    }

    private static void UseToken(IHttpContextAccessor accessor, string token)
        => accessor.HttpContext!.Request.Headers[HeaderNames.Authorization] = $"Bearer {token}";

    // O MESMO token consulta o servidor de autorização UMA vez, por mais endpoints que atenda.
    // É o ganho inteiro da mudança: antes era uma ida por requisição, e duas no approve.
    [Fact]
    public async Task OMesmoToken_ConsultaOServidorUmaVezSo()
    {
        var client = new CountingClient(BillApprove);
        var (cache, accessor, _) = Build(client);
        UseToken(accessor, "token-a");

        for (var i = 0; i < 5; i++)
            Assert.True((await cache.GetAsync()).Snapshot.Grants("bill#approve", ScopesValidationMode.AllOf));

        Assert.Equal(1, client.CallCount);
    }

    // Token DIFERENTE nunca herda o retrato do outro: a chave é o hash do token inteiro, não o
    // 'sub'. Chavear por identidade faria a sessão nova receber as permissões da antiga.
    [Fact]
    public async Task TokenDiferente_NaoHerdaORetratoDoOutro()
    {
        var client = new CountingClient(
            BillApprove,
            RptFetchResult.Resolved(RptSnapshot.From([("bill", ViewScopes)])));
        var (cache, accessor, _) = Build(client);

        UseToken(accessor, "token-a");
        Assert.True((await cache.GetAsync()).Snapshot.Grants("bill#approve", ScopesValidationMode.AllOf));

        UseToken(accessor, "token-b");
        var second = await cache.GetAsync();

        Assert.Equal(2, client.CallCount);
        Assert.False(second.Snapshot.Grants("bill#approve", ScopesValidationMode.AllOf));
        Assert.True(second.Snapshot.Grants("bill#view", ScopesValidationMode.AllOf));
    }

    // Desligado, cada chamada volta a consultar — é o que a suíte de integração usa.
    [Fact]
    public async Task Desligado_ConsultaSempre()
    {
        var client = new CountingClient(BillApprove);
        var (cache, accessor, _) = Build(client, new AuthorizationOptions { RptCacheEnabled = false });
        UseToken(accessor, "token-a");

        await cache.GetAsync();
        await cache.GetAsync();
        await cache.GetAsync();

        Assert.Equal(3, client.CallCount);
    }

    // Falha NÃO fica cacheada: a requisição seguinte tenta de novo, em vez de herdar o erro pelo
    // TTL inteiro. É o oposto do sucesso, e de propósito.
    [Fact]
    public async Task FalhaNaoFicaCacheada()
    {
        var client = new CountingClient(RptFetchResult.Unavailable());
        var (cache, accessor, _) = Build(client, new AuthorizationOptions { RptStaleGrace = TimeSpan.Zero });
        UseToken(accessor, "token-a");

        await cache.GetAsync();
        await cache.GetAsync();

        Assert.Equal(2, client.CallCount);
    }

    // TESTE-ÂNCORA do fail-static: o servidor cai DEPOIS de já ter respondido uma vez, e quem
    // estava usando o sistema continua usando. Sem isto, uma queda do Keycloak vira 503 para todo
    // mundo no instante em que o TTL vence.
    [Fact]
    public async Task ServidorForaDoAr_ServeORetratoAnteriorDentroDaCarencia()
    {
        var client = new CountingClient(BillApprove, RptFetchResult.Unavailable());
        var (cache, accessor, store) = Build(client, new AuthorizationOptions
        {
            RptCacheTtl = TimeSpan.FromMilliseconds(1),
            RptStaleGrace = TimeSpan.FromMinutes(10),
        });
        UseToken(accessor, "token-a");

        Assert.Equal(RptFetchOutcome.Resolved, (await cache.GetAsync()).Outcome);

        await Task.Delay(30);
        var afterOutage = await cache.GetAsync();

        Assert.Equal(RptFetchOutcome.Resolved, afterOutage.Outcome);
        Assert.True(afterOutage.Snapshot.Grants("bill#approve", ScopesValidationMode.AllOf));
        Assert.Equal(2, client.CallCount);
    }

    // Contraprova do anterior: sem carência configurada, indisponibilidade volta a ser
    // indisponibilidade. Degradação silenciosa e eterna seria pior que a queda.
    [Fact]
    public async Task SemCarencia_IndisponibilidadeContinuaSendoIndisponibilidade()
    {
        var client = new CountingClient(BillApprove, RptFetchResult.Unavailable());
        var (cache, accessor, _) = Build(client, new AuthorizationOptions
        {
            RptCacheTtl = TimeSpan.FromMilliseconds(1),
            RptStaleGrace = TimeSpan.Zero,
        });
        UseToken(accessor, "token-a");

        await cache.GetAsync();
        await Task.Delay(30);

        Assert.Equal(RptFetchOutcome.Unavailable, (await cache.GetAsync()).Outcome);
    }

    // Token RECUSADO nunca é servido por retrato velho, mesmo dentro da carência: um token
    // revogado tem que parar de valer na hora, e é a única negativa que o cliente resolve sozinho.
    [Fact]
    public async Task TokenRecusado_NaoEServidoPeloRetratoVelho()
    {
        var client = new CountingClient(BillApprove, RptFetchResult.InvalidToken());
        var (cache, accessor, _) = Build(client, new AuthorizationOptions
        {
            RptCacheTtl = TimeSpan.FromMilliseconds(1),
            RptStaleGrace = TimeSpan.FromMinutes(10),
        });
        UseToken(accessor, "token-a");

        await cache.GetAsync();
        await Task.Delay(30);

        Assert.Equal(RptFetchOutcome.InvalidToken, (await cache.GetAsync()).Outcome);
    }

    // Requisição sem token não vai ao servidor: seria uma ida garantidamente inútil, e o
    // Authorization ausente já é 401 pela autenticação.
    [Fact]
    public async Task SemToken_NaoConsultaOServidor()
    {
        var client = new CountingClient(BillApprove);
        var (cache, _, _) = Build(client);

        Assert.Equal(RptFetchOutcome.InvalidToken, (await cache.GetAsync()).Outcome);
        Assert.Equal(0, client.CallCount);
    }
}
