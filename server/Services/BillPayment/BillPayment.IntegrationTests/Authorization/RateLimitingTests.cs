namespace BillPayment.IntegrationTests.Authorization;

using System.Net;
using System.Net.Http.Json;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

/// <summary>
/// O teto por pessoa nos endpoints que gastam provedor externo.
/// </summary>
/// <remarks>
/// Host próprio com o limitador LIGADO e teto de 2: a fábrica base o desliga porque a suíte
/// bate no mesmo usuário centenas de vezes por minuto, e um teto real faria os outros testes
/// falharem por motivo alheio ao que medem.
/// </remarks>
[Collection(nameof(IntegrationTestCollection))]
public sealed class RateLimitingTests : BaseIntegrationTest, IDisposable
{
    private static readonly Guid Tenant = TestTenants.Primary;
    private static readonly Guid UserA = new("0195a1f0-0000-7000-8000-0000000000a1");
    private static readonly Guid UserB = new("0195a1f0-0000-7000-8000-0000000000a2");

    private readonly WebApplicationFactory<Program> _host;
    private readonly HttpClient _client;

    public RateLimitingTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _host = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Enabled", "true");
            builder.UseSetting("RateLimiting:PermitLimitPerMinute", "1000");
            builder.UseSetting("RateLimiting:ExpensivePermitLimitPerMinute", "2");
        });

        _client = _host.CreateClient().Authenticated();
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
    }

    // Revalidar chama o Asaas a cada requisição. Acima do teto por minuto, 429 com Retry-After —
    // antes de chegar ao controller (o boleto nem existe, e as primeiras respondem 404).
    [Fact]
    public async Task Revalidate_AboveTheExpensiveLimit_ShouldReturnTooManyRequests()
    {
        var first = await RevalidateAsync(UserA);
        var second = await RevalidateAsync(UserA);
        var third = await RevalidateAsync(UserA);

        Assert.Equal(HttpStatusCode.NotFound, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.NotNull(third.Headers.RetryAfter);
    }

    // A partição é a PESSOA: outro usuário do mesmo tenant não paga pelo teto do primeiro.
    [Fact]
    public async Task Revalidate_ByAnotherUser_ShouldNotShareTheLimit()
    {
        await RevalidateAsync(UserA);
        await RevalidateAsync(UserA);

        var other = await RevalidateAsync(UserB);

        Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
    }

    // Leitura comum não entra no teto dos endpoints caros.
    [Fact]
    public async Task GetBills_AfterExhaustingTheExpensiveLimit_ShouldStillAnswer()
    {
        await RevalidateAsync(UserA);
        await RevalidateAsync(UserA);
        await RevalidateAsync(UserA);

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"/api/v1/{Tenant}/bills", UriKind.Relative));
        request.Headers.Add("x-user-id", UserA.ToString());
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Task<HttpResponseMessage> RevalidateAsync(Guid userId)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri($"/api/v1/{Tenant}/bills/{new Guid("0195a1f0-0000-7000-8000-0000000000b9")}/revalidate", UriKind.Relative))
        {
            Content = JsonContent.Create(new { }),
        };

        request.Headers.Add("x-user-id", userId.ToString());
        request.Headers.Add("x-requestid", Guid.CreateVersion7().ToString());

        return _client.SendAsync(request);
    }
}
