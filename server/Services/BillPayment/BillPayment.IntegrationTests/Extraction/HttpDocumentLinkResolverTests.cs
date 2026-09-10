namespace BillPayment.IntegrationTests.Extraction;

using System.Net;
using System.Text;
using BillPayment.Domain.Extraction;
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

    /// <summary>Remetente qualquer: o teto diario por remetente nao e o que estes casos provam.</summary>
    private const string Sender = "faturas@emissor.com.br";

    private static readonly byte[] Pdf = "%PDF-1.4 conteudo do boleto"u8.ToArray();

    // Link direto para PDF em host com receita: uma requisição, e o documento volta com o tipo
    // decidido pelos magic bytes.
    [Fact]
    public async Task Resolve_WithADirectPdfLink_ShouldReturnTheDocument()
    {
        var handler = new RoutingStubHttpMessageHandler().Route("/fatura.pdf", HttpStatusCode.OK, Encoding.Latin1.GetString(Pdf));
        var resolver = Build(handler, Recipe(directDocument: true));

        var resolution = await resolver.ResolveAsync(Body($"https://{Host}/fatura.pdf"), "text/html", Sender, CancellationToken.None);

        Assert.NotNull(resolution.Document);
        Assert.Equal("application/pdf", resolution.Document!.MediaType);
        Assert.Single(handler.Requests);
    }

    // Redirecionamento é o jeito mais simples de burlar a allowlist: conta como não encontrado, e
    // o destino do Location NUNCA é buscado.
    [Fact]
    public async Task Resolve_WhenTheHostRedirects_ShouldNotFollowAndReturnNothing()
    {
        var handler = new RoutingStubHttpMessageHandler().Route("/fatura.pdf", HttpStatusCode.Found, string.Empty);
        var resolver = Build(handler, Recipe(directDocument: true));

        var resolution = await resolver.ResolveAsync(Body($"https://{Host}/fatura.pdf"), "text/html", Sender, CancellationToken.None);

        Assert.Null(resolution.Document);
        Assert.Single(handler.Requests);
    }

    // Resposta acima do teto de bytes é descartada — lida em streaming e abandonada no limite.
    [Fact]
    public async Task Resolve_WhenTheDocumentExceedsMaxBytes_ShouldReturnNothing()
    {
        var oversized = Encoding.Latin1.GetString(Pdf) + new string('x', 5_000);
        var handler = new RoutingStubHttpMessageHandler().Route("/fatura.pdf", HttpStatusCode.OK, oversized);
        var resolver = Build(handler, Recipe(directDocument: true), maxBytes: 1_000);

        var resolution = await resolver.ResolveAsync(Body($"https://{Host}/fatura.pdf"), "text/html", Sender, CancellationToken.None);

        Assert.Null(resolution.Document);
    }

    // O que volta sem magic bytes de PDF e sem tipo de imagem aceito não é documento — é página.
    [Fact]
    public async Task Resolve_WhenTheResponseIsNotADocument_ShouldReturnNothing()
    {
        var handler = new RoutingStubHttpMessageHandler().Route("/fatura.pdf", HttpStatusCode.OK, "<html><body>login</body></html>");
        var resolver = Build(handler, Recipe(directDocument: true));

        var resolution = await resolver.ResolveAsync(Body($"https://{Host}/fatura.pdf"), "text/html", Sender, CancellationToken.None);

        Assert.Null(resolution.Document);
    }

    // Orçamento por mensagem: um e-mail com muitos links autorizados não vira amplificador de
    // tráfego — só MaxFetchesPerMessage requisições saem.
    [Fact]
    public async Task Resolve_WithManyLinks_ShouldStopAtTheFetchBudget()
    {
        var handler = new RoutingStubHttpMessageHandler().Route("/doc", HttpStatusCode.NotFound, string.Empty);
        var resolver = Build(handler, Recipe(directDocument: true), maxFetches: 2);
        var body = Body(Enumerable.Range(1, 6).Select(i => $"https://{Host}/doc{i}.pdf").ToArray());

        var resolution = await resolver.ResolveAsync(body, "text/html", Sender, CancellationToken.None);

        Assert.Null(resolution.Document);
        Assert.Equal(2, handler.Requests.Count);
    }

    // Host sem receita não é buscado — nenhuma requisição sai, nem para conferir.
    [Fact]
    public async Task Resolve_WithALinkToAHostWithoutARecipe_ShouldNotSendAnyRequest()
    {
        var handler = new RoutingStubHttpMessageHandler().Route("/fatura.pdf", HttpStatusCode.OK, Encoding.Latin1.GetString(Pdf));
        var resolver = Build(handler, Recipe(directDocument: true));

        var resolution = await resolver.ResolveAsync(Body("https://1.1.1.1/fatura.pdf"), "text/html", Sender, CancellationToken.None);

        Assert.Null(resolution.Document);
        Assert.Empty(handler.Requests);
    }

    // TESTE-ÂNCORA do caso Acessórias: a página do emissor não tem âncora nenhuma e entrega o PDF
    // por `document.write("<iframe src=...>")` num OUTRO host. A escada tem que achar em dois
    // saltos — e a procedência gravada tem que ser o link do E-MAIL, não a URL presignada do
    // segundo salto, que expira em dois minutos e estaria morta na quarentena.
    [Fact]
    public async Task Resolve_WhenTheDocumentIsBehindAScriptWrittenIframe_ShouldFindItAndReportTheEmailLink()
    {
        const string emailLink = $"https://{Host}/getguia.php?ko=abc";
        const string fileUrl = $"https://{FollowHost}/x/260902.pdf?X-Amz-Expires=120";

        var handler = new RoutingStubHttpMessageHandler()
            .Route("/getguia.php", HttpStatusCode.OK,
                $"""<html><body><script>document.write("<iframe src='{fileUrl}' />");</script></body></html>""",
                "text/html")
            .Route("/x/260902.pdf", HttpStatusCode.OK, Encoding.Latin1.GetString(Pdf), "application/pdf");

        var resolver = Build(handler, FollowRecipe());

        var resolution = await resolver.ResolveAsync(Body(emailLink), "text/html", Sender, CancellationToken.None);

        Assert.Equal(LinkResolutionOutcome.Resolved, resolution.Outcome);
        Assert.Equal("application/pdf", resolution.Document!.MediaType);
        Assert.Equal(emailLink, resolution.BestCandidate!.Url);
        Assert.Equal(2, resolution.DeepestLevel);
    }

    // No regime aberto o destino sai do e-mail, e é isso que permite descobrir emissor novo sem
    // cadastro manual — o motivo de o modo existir.
    [Fact]
    public async Task Resolve_InOpenMode_ShouldReachAHostWithoutAnyRecipe()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route("/boleto.pdf", HttpStatusCode.OK, Encoding.Latin1.GetString(Pdf), "application/pdf");

        var resolver = BuildOpen(handler);

        var resolution = await resolver.ResolveAsync(
            Body($"https://{Host}/boleto.pdf"), "text/html", Sender, CancellationToken.None);

        Assert.Equal(LinkResolutionOutcome.Resolved, resolution.Outcome);
    }

    // A profundidade limita a FORMA da árvore, e o desfecho tem que dizer isso: "desci o quanto
    // podia e não achei" é informação diferente de "o emissor não tem receita".
    [Fact]
    public async Task Resolve_WhenTheLadderRunsOutOfDepth_ShouldReportDepthExhausted()
    {
        var resolver = BuildOpen(Chain(out var handler), maxDepth: 2, maxFetches: 50, maxPerHost: 50);

        var resolution = await resolver.ResolveAsync(
            Body($"https://{Host}/nivel0"), "text/html", Sender, CancellationToken.None);

        Assert.Equal(LinkResolutionOutcome.DepthExhausted, resolution.Outcome);
        Assert.Equal(2, resolution.DeepestLevel);
        Assert.Equal(2, handler.Requests.Count);
    }

    // O ORÇAMENTO é o que impede a explosão combinatória — profundidade sozinha não segura volume.
    [Fact]
    public async Task Resolve_WhenTheBudgetRunsOutBeforeTheDepth_ShouldReportBudgetExhausted()
    {
        var resolver = BuildOpen(Chain(out var handler), maxDepth: 5, maxFetches: 2, maxPerHost: 50);

        var resolution = await resolver.ResolveAsync(
            Body($"https://{Host}/nivel0"), "text/html", Sender, CancellationToken.None);

        Assert.Equal(LinkResolutionOutcome.BudgetExhausted, resolution.Outcome);
        Assert.Equal(2, handler.Requests.Count);
    }

    // Duas páginas que se apontam são um laço: sem conjunto de visitados a escada giraria nelas
    // até o orçamento acabar, gastando as requisições que o documento precisaria.
    [Fact]
    public async Task Resolve_WhenTwoPagesPointAtEachOther_ShouldNotFetchTheSameUrlTwice()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route("/pagina-a", HttpStatusCode.OK, Page($"https://{Host}/pagina-b"), "text/html")
            .Route("/pagina-b", HttpStatusCode.OK, Page($"https://{Host}/pagina-a"), "text/html");

        var resolver = BuildOpen(handler, maxDepth: 5, maxFetches: 20, maxPerHost: 20);

        await resolver.ResolveAsync(Body($"https://{Host}/pagina-a"), "text/html", Sender, CancellationToken.None);

        Assert.Equal(2, handler.Requests.Count);
    }

    // Porta fora da lista é recusada ANTES de qualquer requisição: é a defesa em profundidade que
    // sobra se a conferência de faixa de IP falhar — 6379, 5432 e 11211 são o que um atacante quer.
    [Fact]
    public async Task Resolve_InOpenModeWithAnUnlistedPort_ShouldRefuseWithoutTouchingTheNetwork()
    {
        var handler = new RoutingStubHttpMessageHandler();
        var resolver = BuildOpen(handler);

        var resolution = await resolver.ResolveAsync(
            Body($"https://{Host}:6379/boleto.pdf"), "text/html", Sender, CancellationToken.None);

        Assert.Equal(LinkResolutionOutcome.Refused, resolution.Outcome);
        Assert.Empty(handler.Requests);
    }

    // TESTE-ÂNCORA do achado C1: a imagem era aceita pelo Content-Type que o servidor REMOTO
    // declarava. Um servidor hostil respondendo image/png com qualquer coisa entregava esses bytes
    // ao decodificador de imagem, ao extrator de IA e ao aparelho de quem aprova.
    [Fact]
    public async Task Resolve_WhenTheServerLiesAboutBeingAnImage_ShouldRefuseTheBytes()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route("/boleto.png", HttpStatusCode.OK, "<html>isto nao e uma imagem</html>", "image/png");

        var resolver = BuildOpen(handler);

        var resolution = await resolver.ResolveAsync(
            Body($"https://{Host}/boleto.png"), "text/html", Sender, CancellationToken.None);

        Assert.Null(resolution.Document);
    }

    // CONTRAPROVA do anterior: imagem de verdade passa, e o tipo sai dos BYTES, não do cabeçalho.
    [Fact]
    public async Task Resolve_WithARealPngServedAsOctetStream_ShouldAcceptItByItsMagicBytes()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .RouteBytes("/boleto", HttpStatusCode.OK,
                [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00], "application/octet-stream");

        var resolver = BuildOpen(handler);

        var resolution = await resolver.ResolveAsync(
            Body($"https://{Host}/boleto"), "text/html", Sender, CancellationToken.None);

        Assert.Equal("image/png", resolution.Document!.MediaType);
    }

    // O teto diário por remetente: o orçamento por mensagem limita um e-mail, este limita mil.
    [Fact]
    public async Task Resolve_WhenTheSenderExceedsTheDailyCap_ShouldReportThrottled()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route("/boleto.pdf", HttpStatusCode.OK, Encoding.Latin1.GetString(Pdf), "application/pdf");

        var resolver = BuildOpen(handler, perSenderPerDay: 1);
        var body = Body($"https://{Host}/boleto.pdf");

        Assert.Equal(LinkResolutionOutcome.Resolved,
            (await resolver.ResolveAsync(body, "text/html", Sender, CancellationToken.None)).Outcome);

        Assert.Equal(LinkResolutionOutcome.Throttled,
            (await resolver.ResolveAsync(body, "text/html", Sender, CancellationToken.None)).Outcome);
    }

    private const string FollowHost = "9.9.9.9";

    /// <summary>
    /// Uma página que aponta para o endereço seguinte.
    /// </summary>
    /// <remarks>
    /// O invólucro <c>&lt;html&gt;&lt;body&gt;</c> não é enfeite: <c>HtmlText.LooksLikeHtml</c>
    /// decide pelos bytes se vale colher o salto seguinte, e um fragmento solto de âncora não
    /// passa nessa sonda — a escada pararia ali sem nunca descer.
    /// </remarks>
    private static string Page(string next)
        => $"""<html><body><a href="{next}">continua</a></body></html>""";

    /// <summary>
    /// Uma corrente de páginas: cada nível aponta para o próximo, e nenhum entrega documento.
    /// </summary>
    private static RoutingStubHttpMessageHandler Chain(out RoutingStubHttpMessageHandler handler)
    {
        handler = new RoutingStubHttpMessageHandler();

        for (var level = 0; level < 6; level++)
            handler.Route($"/nivel{level}", HttpStatusCode.OK, Page($"https://{Host}/nivel{level + 1}"), "text/html");

        return handler;
    }

    private static ReadOnlyMemory<byte> Body(params string[] urls)
        => Encoding.UTF8.GetBytes("<html><body>" + string.Concat(urls.Select(u => $"<a href=\"{u}\">Boleto</a>")) + "</body></html>");

    private static LinkRecipe Recipe(bool directDocument)
        => new() { Host = Host, Port = 443, DirectDocument = directDocument };

    private static LinkRecipe FollowRecipe()
        => new() { Host = Host, Port = 443, DirectDocument = false, FollowHosts = [FollowHost] };

    private static HttpDocumentLinkResolver Build(
        RoutingStubHttpMessageHandler handler, LinkRecipe recipe, int maxBytes = 1_000_000, int maxFetches = 4)
        => Build(new LinkResolutionOptions
        {
            Enabled = true,
            MaxBytes = maxBytes,
            MaxFetchesPerMessage = maxFetches,
            Recipes = [recipe],
        }, handler);

    /// <summary>O regime aberto: sem receita nenhuma, e o destino saindo do e-mail.</summary>
    private static HttpDocumentLinkResolver BuildOpen(
        RoutingStubHttpMessageHandler handler,
        int maxDepth = 5,
        int maxFetches = 12,
        int maxPerHost = 3,
        int perSenderPerDay = 200)
        => Build(new LinkResolutionOptions
        {
            Enabled = true,
            Mode = LinkResolutionMode.Open,
            MaxDepth = maxDepth,
            MaxFetchesPerMessage = maxFetches,
            MaxFetchesPerHost = maxPerHost,
            MaxFetchesPerSenderPerDay = perSenderPerDay,
            Recipes = [],
        }, handler);

    private static HttpDocumentLinkResolver Build(
        LinkResolutionOptions value, RoutingStubHttpMessageHandler handler)
    {
        var options = Options.Create(value);

        return new HttpDocumentLinkResolver(
            new StubHttpClientFactory(handler),
            new SenderFetchBudget(options, TimeProvider.System),
            options,
            NullLogger<HttpDocumentLinkResolver>.Instance);
    }
}
