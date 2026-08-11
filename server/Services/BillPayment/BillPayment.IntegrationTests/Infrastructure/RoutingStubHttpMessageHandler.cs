namespace BillPayment.IntegrationTests.Infrastructure;

using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Transporte de teste que responde de acordo com a URL pedida.
/// </summary>
/// <remarks>
/// O adapter do Graph faz três conversas diferentes numa varredura — token, delta e anexos — e
/// um stub de resposta única não conseguiria exercitar nenhuma delas de forma honesta. O que
/// está sob teste continua sendo a <strong>tradução</strong> das respostas; o que é substituído
/// é o transporte.
/// </remarks>
internal sealed class RoutingStubHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(Func<Uri, bool> Matches, Func<int, (HttpStatusCode Status, string Body)> Respond)> _routes = [];
    private readonly Dictionary<string, int> _hits = new(StringComparer.Ordinal);

    public List<Uri> Requests { get; } = [];

    /// <summary>Responde sempre o mesmo para URLs que contenham <paramref name="fragment"/>.</summary>
    public RoutingStubHttpMessageHandler Route(string fragment, HttpStatusCode status, string body)
    {
        _routes.Add((uri => uri.ToString().Contains(fragment, StringComparison.OrdinalIgnoreCase), _ => (status, body)));
        return this;
    }

    /// <summary>
    /// Responde de forma diferente a cada acerto, para exercitar paginação — a n-ésima chamada
    /// que casa recebe o n-ésimo corpo, e o último se repete depois disso.
    /// </summary>
    public RoutingStubHttpMessageHandler RouteSequence(string fragment, params string[] bodies)
    {
        _routes.Add((
            uri => uri.ToString().Contains(fragment, StringComparison.OrdinalIgnoreCase),
            hit => (HttpStatusCode.OK, bodies[Math.Min(hit, bodies.Length - 1)])));

        return this;
    }

    public int HitsFor(string fragment) => _hits.GetValueOrDefault(fragment, 0);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri!;
        Requests.Add(uri);

        foreach (var (matches, respond) in _routes)
        {
            if (!matches(uri))
                continue;

            var key = uri.AbsolutePath;
            var hit = _hits.GetValueOrDefault(key, 0);
            _hits[key] = hit + 1;

            var (status, body) = respond(hit);

            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("""{"error":{"code":"unrouted","message":"stub sem rota"}}""",
                Encoding.UTF8, "application/json"),
        });
    }
}

/// <summary>Fábrica que entrega o mesmo transporte de teste para qualquer cliente nomeado.</summary>
internal sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
}
