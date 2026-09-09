namespace BillPayment.IntegrationTests.Payments;

using System.Net;
using BillPayment.Infra.Payments;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// A URL do comprovante vem do provedor, mas é dado de fora mesmo assim: a
/// <c>SafeUrlPolicy</c> da escada de links vale aqui também — sem ela, um retrato malicioso
/// apontaria o fetch para a rede interna ou para os metadados de nuvem (SSRF).
/// </summary>
public sealed class HttpPaymentReceiptFetcherTests
{
    // Host interno (literal, sem DNS) é recusado ANTES de qualquer requisição sair.
    [Theory]
    [InlineData("http://127.0.0.1/comprovante.pdf")]
    [InlineData("https://169.254.169.254/latest/meta-data")]
    [InlineData("https://10.0.0.8/interno")]
    [InlineData("https://192.168.15.40/nas")]
    public async Task FetchAsync_WithAnInternalHost_ShouldRefuseWithoutTouchingTheNetwork(string url)
    {
        var handler = StubHttpMessageHandler.Ok("conteudo");
        var fetcher = BuildFetcher(handler);

        var result = await fetcher.FetchAsync(url, CancellationToken.None);

        Assert.False(result.IsFetched);
        Assert.False(result.IsRetryable);
        Assert.Equal("unsafe_receipt_url", result.ReasonCode);
        Assert.Equal(0, handler.RequestCount);
    }

    // URL que nem é URL continua sendo o desfecho definitivo de sempre.
    [Fact]
    public async Task FetchAsync_WithAMalformedUrl_ShouldRefuse()
    {
        var fetcher = BuildFetcher(StubHttpMessageHandler.Ok("conteudo"));

        var result = await fetcher.FetchAsync("not a url", CancellationToken.None);

        Assert.False(result.IsFetched);
        Assert.Equal("malformed_receipt_url", result.ReasonCode);
    }

    // O caminho real medido em 2026-09-09: a URL do provedor serve a PÁGINA, e o comprovante é
    // o PDF que ela oferece numa âncora. Sem o salto, o balde recebia o HTML e a tela do
    // usuário ficava vazia. As âncoras de CSS e de rodapé vêm antes de propósito — quem escolhe
    // é o formato do caminho, nunca a ordem na página.
    [Fact]
    public async Task FetchAsync_WhenTheProviderServesThePageWithALinkToTheFile_ShouldStoreThePdf()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route(RECEIPT_PATH, HttpStatusCode.OK, ReceiptPageWith(RELATIVE_PDF_HREF), HTML)
            .Route(PDF_PATH, HttpStatusCode.OK, PDF_BODY, PDF);

        var result = await BuildFetcher(handler).FetchAsync(PAGE_URL, CancellationToken.None);

        Assert.True(result.IsFetched);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(PDF_BODY, Body(result));
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(PDF_PATH, handler.Requests[1].AbsolutePath);
    }

    // Provedor que serve o arquivo direto não paga nada pelo salto — e o tipo sai do conteúdo
    // (%PDF-), não do cabeçalho, que aqui mente dizendo JSON.
    [Fact]
    public async Task FetchAsync_WhenTheProviderServesTheFileItself_ShouldNotFollowAnyLink()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route(RECEIPT_PATH, HttpStatusCode.OK, PDF_BODY);

        var result = await BuildFetcher(handler).FetchAsync(PAGE_URL, CancellationToken.None);

        Assert.True(result.IsFetched);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Single(handler.Requests);
    }

    // Sem regressão: página que não oferece o arquivo continua sendo a evidência guardada, como
    // antes de o salto existir.
    [Fact]
    public async Task FetchAsync_WhenThePageOffersNoLinkToTheFile_ShouldKeepThePageItself()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route(RECEIPT_PATH, HttpStatusCode.OK, ReceiptPageWith(href: null), HTML);

        var result = await BuildFetcher(handler).FetchAsync(PAGE_URL, CancellationToken.None);

        Assert.True(result.IsFetched);
        Assert.Equal(HTML, result.ContentType);
        Assert.Single(handler.Requests);
    }

    // A trava que fecha o SSRF por HTML hostil: o link só é seguido dentro do host da página,
    // que é quem passou pela SafeUrlPolicy. Nenhuma requisição sai para o outro endereço.
    [Fact]
    public async Task FetchAsync_WhenTheLinkPointsToAnotherHost_ShouldRefuseItAndKeepThePage()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route(RECEIPT_PATH, HttpStatusCode.OK, ReceiptPageWith(FOREIGN_PDF_HREF), HTML)
            .Route(FOREIGN_HOST, HttpStatusCode.OK, PDF_BODY, PDF);

        var result = await BuildFetcher(handler).FetchAsync(PAGE_URL, CancellationToken.None);

        Assert.True(result.IsFetched);
        Assert.Equal(HTML, result.ContentType);
        Assert.Single(handler.Requests);
        Assert.DoesNotContain(handler.Requests, uri => uri.Host == FOREIGN_HOST);
    }

    // Falha passageira no arquivo NÃO vira "guarda a página": gravar o HTML aqui o congelaria no
    // lugar do PDF e tiraria a ordem da varredura. Retentável mantém o BLP.PMO21 e a 2ª chance.
    [Fact]
    public async Task FetchAsync_WhenTheFileIsMomentarilyUnavailable_ShouldStayRetryable()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route(RECEIPT_PATH, HttpStatusCode.OK, ReceiptPageWith(RELATIVE_PDF_HREF), HTML)
            .Route(PDF_PATH, HttpStatusCode.ServiceUnavailable, string.Empty, PDF);

        var result = await BuildFetcher(handler).FetchAsync(PAGE_URL, CancellationToken.None);

        Assert.False(result.IsFetched);
        Assert.True(result.IsRetryable);
        Assert.Equal("http_503", result.ReasonCode);
    }

    // Link que responde 200 com qualquer outra coisa não vira comprovante: quem decide que é PDF
    // é o %PDF- do corpo. Sem isso, uma página de erro seria gravada com extensão .pdf.
    [Fact]
    public async Task FetchAsync_WhenTheLinkDoesNotRenderAPdf_ShouldKeepThePage()
    {
        var handler = new RoutingStubHttpMessageHandler()
            .Route(RECEIPT_PATH, HttpStatusCode.OK, ReceiptPageWith(RELATIVE_PDF_HREF), HTML)
            .Route(PDF_PATH, HttpStatusCode.OK, "<html><body>indisponivel</body></html>", HTML);

        var result = await BuildFetcher(handler).FetchAsync(PAGE_URL, CancellationToken.None);

        Assert.True(result.IsFetched);
        Assert.Equal(HTML, result.ContentType);
        Assert.Equal(2, handler.Requests.Count);
    }

    private const string HTML = "text/html";
    private const string PDF = "application/pdf";

    // Endereço literal e público (TEST-NET-3): passa pela SafeUrlPolicy sem consultar DNS, o que
    // mantém o teste offline e determinístico.
    private const string HOST = "203.0.113.10";
    private const string FOREIGN_HOST = "198.51.100.7";

    private const string RECEIPT_PATH = "/comprovantes/h/UElYX1RSQU5TQUNUSU9OX0RPTkU";
    private const string PDF_PATH = "/transactionReceipt/pdf/7077419209677481";

    private const string PAGE_URL = $"https://{HOST}{RECEIPT_PATH}";
    private const string RELATIVE_PDF_HREF = PDF_PATH;
    private const string FOREIGN_PDF_HREF = $"https://{FOREIGN_HOST}{PDF_PATH}";

    private const string PDF_BODY = "%PDF-1.4\nconteudo do comprovante\n%%EOF";

    /// <summary>
    /// A página como o provedor a monta: folhas de estilo e script antes, o botão do arquivo
    /// depois. É o que faz "pegue o primeiro link" ser a resposta errada.
    /// </summary>
    private static string ReceiptPageWith(string? href)
    {
        var button = href is null
            ? "<span>Comprovante indisponivel</span>"
            : $"""<a class="mdl-button primary-button" href="{href}">Baixar pdf</a>""";

        return $"""
            <html><head>
            <link href="/assets/transactionReceipt.css" rel="stylesheet" />
            </head><body>
            <a href="/assets/loading.gif">carregando</a>
            <div class="download-button-container">{button}</div>
            </body></html>
            """;
    }

    private static string Body(Domain.Ports.ReceiptFetchResult result)
        => System.Text.Encoding.UTF8.GetString(result.Content!.Value.Span);

    private static HttpPaymentReceiptFetcher BuildFetcher(HttpMessageHandler handler)
        => new(
            new StubHttpClientFactory(handler),
            NullLogger<HttpPaymentReceiptFetcher>.Instance);
}
