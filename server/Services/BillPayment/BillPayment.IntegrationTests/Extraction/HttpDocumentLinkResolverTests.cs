namespace BillPayment.IntegrationTests.Extraction;

using System.Net;
using System.Text;
using BillPayment.Infra.Extraction.Links;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// As quatro travas da escada de link, no RESOLVEDOR de verdade — sem rede, com transporte falso.
/// </summary>
/// <remarks>
/// Até 2026-08-28 só a política de endereço tinha teste; o resolvedor, onde vivem a recusa de
/// redirecionamento, o teto de bytes, a conferência de magic bytes e o orçamento por mensagem,
/// estava a 0 % — coberto apenas pelo dublê. O host é um IP público literal porque a política
/// resolve DNS, e a suíte não pode depender de rede.
/// </remarks>
public sealed class HttpDocumentLinkResolverTests
{
    private const string Host = "8.8.8.8";

    private static readonly byte[] Pdf = "%PDF-1.4 conteudo do boleto"u8.ToArray();

    // Link direto para PDF em host com receita: uma requisição, e o documento volta com o tipo
    // decidido pelos magic bytes.
    [Fact]
    public async Task Resolve_WithADirectPdfLink_ShouldReturnTheDocument()
    {
        var handler = new RoutingStubHttpMessageHandler().Route("/fatura.pdf", HttpStatusCode.OK, Encoding.Latin1.GetString(Pdf));
        var resolver = Build(handler, Recipe(directDocument: true));

        var document = await resolver.ResolveAsync(Body($"https://{Host}/fatura.pdf"), "text/html", CancellationToken.None);

        Assert.NotNull(document);
        Assert.Equal("application/pdf", document!.MediaType);
        Assert.Single(handler.Requests);
    }

    // Redirecionamento é o jeito mais simples de burlar a allowlist: conta como não encontrado, e
    // o destino do Location NUNCA é buscado.
    [Fact]
    public async Task Resolve_WhenTheHostRedirects_ShouldNotFollowAndReturnNothing()
    {
        var handler = new RoutingStubHttpMessageHandler().Route("/fatura.pdf", HttpStatusCode.Found, string.Empty);
        var resolver = Build(handler, Recipe(directDocument: true));

        var document = await resolver.ResolveAsync(Body($"https://{Host}/fatura.pdf"), "text/html", CancellationToken.None);

        Assert.Null(document);
        Assert.Single(handler.Requests);
    }

    // Resposta acima do teto de bytes é descartada — lida em streaming e abandonada no limite.
    [Fact]
    public async Task Resolve_WhenTheDocumentExceedsMaxBytes_ShouldReturnNothing()
    {
        var oversized = Encoding.Latin1.GetString(Pdf) + new string('x', 5_000);
        var handler = new RoutingStubHttpMessageHandler().Route("/fatura.pdf", HttpStatusCode.OK, oversized);
        var resolver = Build(handler, Recipe(directDocument: true), maxBytes: 1_000);

        var document = await resolver.ResolveAsync(Body($"https://{Host}/fatura.pdf"), "text/html", CancellationToken.None);

        Assert.Null(document);
    }

    // O que volta sem magic bytes de PDF e sem tipo de imagem aceito não é documento — é página.
    [Fact]
    public async Task Resolve_WhenTheResponseIsNotADocument_ShouldReturnNothing()
    {
        var handler = new RoutingStubHttpMessageHandler().Route("/fatura.pdf", HttpStatusCode.OK, "<html><body>login</body></html>");
        var resolver = Build(handler, Recipe(directDocument: true));

        var document = await resolver.ResolveAsync(Body($"https://{Host}/fatura.pdf"), "text/html", CancellationToken.None);

        Assert.Null(document);
    }

    // Orçamento por mensagem: um e-mail com muitos links autorizados não vira amplificador de
    // tráfego — só MaxFetchesPerMessage requisições saem.
    [Fact]
    public async Task Resolve_WithManyLinks_ShouldStopAtTheFetchBudget()
    {
        var handler = new RoutingStubHttpMessageHandler().Route("/doc", HttpStatusCode.NotFound, string.Empty);
        var resolver = Build(handler, Recipe(directDocument: true), maxFetches: 2);
        var body = Body(Enumerable.Range(1, 6).Select(i => $"https://{Host}/doc{i}.pdf").ToArray());

        var document = await resolver.ResolveAsync(body, "text/html", CancellationToken.None);

        Assert.Null(document);
        Assert.Equal(2, handler.Requests.Count);
    }

    // Host sem receita não é buscado — nenhuma requisição sai, nem para conferir.
    [Fact]
    public async Task Resolve_WithALinkToAHostWithoutARecipe_ShouldNotSendAnyRequest()
    {
        var handler = new RoutingStubHttpMessageHandler().Route("/fatura.pdf", HttpStatusCode.OK, Encoding.Latin1.GetString(Pdf));
        var resolver = Build(handler, Recipe(directDocument: true));

        var document = await resolver.ResolveAsync(Body("https://1.1.1.1/fatura.pdf"), "text/html", CancellationToken.None);

        Assert.Null(document);
        Assert.Empty(handler.Requests);
    }

    private static ReadOnlyMemory<byte> Body(params string[] urls)
        => Encoding.UTF8.GetBytes("<html><body>" + string.Concat(urls.Select(u => $"<a href=\"{u}\">Boleto</a>")) + "</body></html>");

    private static LinkRecipe Recipe(bool directDocument)
        => new() { Host = Host, Port = 443, DirectDocument = directDocument };

    private static HttpDocumentLinkResolver Build(
        RoutingStubHttpMessageHandler handler, LinkRecipe recipe, int maxBytes = 1_000_000, int maxFetches = 4)
        => new(
            new StubHttpClientFactory(handler),
            Options.Create(new LinkResolutionOptions
            {
                Enabled = true,
                MaxBytes = maxBytes,
                MaxFetchesPerMessage = maxFetches,
                Recipes = [recipe],
            }),
            NullLogger<HttpDocumentLinkResolver>.Instance);
}
