namespace BillPayment.Infra.Payments;

using System.Text;
using BillPayment.Domain.Ports;
using BillPayment.Infra.Extraction.Links;
using Microsoft.Extensions.Logging;

/// <summary>
/// Baixa o comprovante pela URL do provedor — e, quando ela devolve a PÁGINA do comprovante,
/// atravessa até o arquivo que a página oferece.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Só o host entra em log</strong> — a URL do comprovante é credencial ao portador,
/// como toda URL de documento deste BC (gotchas). Cliente próprio, sem retry: a retentativa é
/// da reentrega do outbox, com backoff.
/// </para>
/// <para>
/// <strong>O segundo salto tem orçamento de UM e nunca sai do host da página.</strong> Medido em
/// 2026-09-09: <c>transactionReceiptUrl</c> serve <c>text/html</c>, e o arquivo está numa âncora
/// dentro dela (ver <see cref="AsaasReceiptPdfLink"/>). Sem o salto, o que ia para o balde era a
/// página — que o app não sabe renderizar. Sem o teto e sem a trava de host, o HTML de fora
/// decidiria para onde a aplicação vai.
/// </para>
/// <para>
/// <strong>Quem decide que é PDF é o conteúdo, não o cabeçalho.</strong> O <c>content-type</c> vem
/// do provedor; o <c>%PDF-</c> vem do arquivo. Declarar o tipo pelo que foi medido é o que impede
/// de gravar um <c>.pdf</c> em cima de uma página de erro que responde 200.
/// </para>
/// <para>
/// Teto de tamanho porque o download vai inteiro para a memória antes do balde — e um
/// comprovante real tem dezenas de KB, não dezenas de MB.
/// </para>
/// </remarks>
internal sealed class HttpPaymentReceiptFetcher(
    IHttpClientFactory httpClientFactory,
    SafeUrlPolicy safeUrl,
    ILogger<HttpPaymentReceiptFetcher> logger) : IPaymentReceiptFetcher
{
    public const string CLIENT_NAME = "asaas-receipt";

    private const string PDF_CONTENT_TYPE = "application/pdf";

    private const long MAX_BYTES = 10 * 1024 * 1024;

    /// <summary>Quanto do corpo basta para reconhecer HTML sem varrer megabytes.</summary>
    private const int SNIFF_BYTES = 512;

    private static ReadOnlySpan<byte> PdfMagic => "%PDF-"u8;

    public async Task<ReceiptFetchResult> FetchAsync(string receiptUrl, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(receiptUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return ReceiptFetchResult.NotFound("malformed_receipt_url");
        }

        // A URL vem do provedor, mas é dado de fora mesmo assim — a mesma SafeUrlPolicy da
        // escada de links fecha o SSRF (host interno, metadados de nuvem, rebinding). Recusar é
        // desfecho definitivo, não indisponibilidade: a URL não vai melhorar.
        if (await safeUrl.ResolvePinnedAddressAsync(uri.Host, cancellationToken) is null)
        {
            logger.LogWarning("Comprovante em {Host} recusado pela política de URL segura.", uri.Host);
            return ReceiptFetchResult.NotFound("unsafe_receipt_url");
        }

        var http = httpClientFactory.CreateClient(CLIENT_NAME);

        var landing = await GetAsync(http, uri, cancellationToken);

        return landing.IsFetched
            ? await ResolveDocumentAsync(http, uri, landing, cancellationToken)
            : landing;
    }

    /// <summary>
    /// A resposta da URL do provedor já é o arquivo, ou é a página que aponta para ele?
    /// </summary>
    private async Task<ReceiptFetchResult> ResolveDocumentAsync(
        HttpClient http,
        Uri pageUri,
        ReceiptFetchResult landing,
        CancellationToken cancellationToken)
    {
        var content = landing.Content!.Value;

        // Provedor que serve o arquivo direto não paga nada pelo salto.
        if (IsPdf(content.Span))
            return ReceiptFetchResult.Fetched(content, PDF_CONTENT_TYPE);

        if (!LooksLikeHtml(landing.ContentType, content.Span))
            return landing;

        var link = AsaasReceiptPdfLink.TryResolve(Encoding.UTF8.GetString(content.Span), pageUri);

        if (link is null)
        {
            // Sem regressão: a página continua sendo a evidência guardada, como antes de o salto
            // existir. O aviso é o que faz alguém olhar se o provedor mudou o layout.
            logger.LogWarning(
                "A página do comprovante em {Host} não oferece link para o arquivo; a própria página será guardada.",
                pageUri.Host);

            return landing;
        }

        var document = await GetAsync(http, link, cancellationToken);

        // Falha passageira no arquivo NÃO vira "guarda a página": a página seria gravada para
        // sempre no lugar do PDF, e a varredura pararia de procurar. Devolver retentável mantém
        // o BLP.PMO21 e a segunda chance.
        if (document.IsRetryable)
            return document;

        if (!document.IsFetched || !IsPdf(document.Content!.Value.Span))
        {
            logger.LogWarning(
                "O link do arquivo do comprovante em {Host} não rendeu um PDF ({Reason}); a página será guardada.",
                pageUri.Host,
                document.ReasonCode ?? "not_a_pdf");

            return landing;
        }

        return ReceiptFetchResult.Fetched(document.Content.Value, PDF_CONTENT_TYPE);
    }

    private async Task<ReceiptFetchResult> GetAsync(HttpClient http, Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                logger.LogWarning("Comprovante em {Host} respondeu {Status}.", uri.Host, status);

                return status is 404 or 410
                    ? ReceiptFetchResult.NotFound($"http_{status}")
                    : ReceiptFetchResult.Unavailable($"http_{status}");
            }

            if (response.Content.Headers.ContentLength is > MAX_BYTES)
                return ReceiptFetchResult.NotFound("receipt_too_large");

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.LongLength > MAX_BYTES)
                return ReceiptFetchResult.NotFound("receipt_too_large");

            return bytes.Length == 0
                ? ReceiptFetchResult.NotFound("empty_receipt")
                : ReceiptFetchResult.Fetched(bytes, response.Content.Headers.ContentType?.MediaType);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
            && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Comprovante em {Host} não obteve resposta.", uri.Host);
            return ReceiptFetchResult.Unavailable("transport_error");
        }
    }

    private static bool IsPdf(ReadOnlySpan<byte> content) => content.StartsWith(PdfMagic);

    /// <summary>
    /// Cabeçalho primeiro; corpo quando ele não diz nada. Provedor que devolve
    /// <c>application/octet-stream</c> numa página existe, e perder o salto por causa disso
    /// custaria o comprovante.
    /// </summary>
    private static bool LooksLikeHtml(string? contentType, ReadOnlySpan<byte> content)
    {
        if (contentType?.Contains("html", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        var head = Encoding.ASCII.GetString(content[..Math.Min(content.Length, SNIFF_BYTES)]);

        return head.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || head.Contains("<!doctype", StringComparison.OrdinalIgnoreCase);
    }
}
