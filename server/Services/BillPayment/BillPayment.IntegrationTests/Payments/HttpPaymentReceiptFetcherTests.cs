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

    private static HttpPaymentReceiptFetcher BuildFetcher(StubHttpMessageHandler handler)
        => new(
            new StubHttpClientFactory(handler),
            NullLogger<HttpPaymentReceiptFetcher>.Instance);
}
