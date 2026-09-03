namespace BillPayment.Infra.Payments;

using BillPayment.Domain.Ports;
using BillPayment.Infra.Extraction.Links;
using Microsoft.Extensions.Logging;

/// <summary>
/// Baixa o comprovante pela URL do provedor.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Só o host entra em log</strong> — a URL do comprovante é credencial ao portador,
/// como toda URL de documento deste BC (gotchas). Cliente próprio, sem retry: a retentativa é
/// da reentrega do outbox, com backoff.
/// </para>
/// <para>
/// Teto de tamanho porque o download vai inteiro para a memória antes do balde — e um
/// comprovante real tem dezenas de KB, não dezenas de MB.
/// </para>
/// </remarks>
internal sealed class HttpPaymentReceiptFetcher(
    IHttpClientFactory httpClientFactory,
    ILogger<HttpPaymentReceiptFetcher> logger) : IPaymentReceiptFetcher
{
    public const string CLIENT_NAME = "asaas-receipt";

    private const long MAX_BYTES = 10 * 1024 * 1024;

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
        if (!await SafeUrlPolicy.IsPubliclyRoutableAsync(uri.Host, cancellationToken))
        {
            logger.LogWarning("Comprovante em {Host} recusado pela política de URL segura.", uri.Host);
            return ReceiptFetchResult.NotFound("unsafe_receipt_url");
        }

        var http = httpClientFactory.CreateClient(CLIENT_NAME);

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
}
