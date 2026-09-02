namespace BillPayment.IntegrationTests.Infrastructure;

using BillPayment.Domain.Ports;

/// <summary>Comprovante determinístico — a URL do provedor é ENTRADA do fluxo sob teste.</summary>
internal sealed class FakeReceiptFetcher : IPaymentReceiptFetcher
{
    public static readonly byte[] DefaultReceipt = "%PDF-1.4 comprovante-fake"u8.ToArray();

    public ReceiptFetchResult Scripted { get; set; } = ReceiptFetchResult.Fetched(DefaultReceipt, "application/pdf");

    public string? LastUrl { get; private set; }

    public int Calls { get; private set; }

    public void Reset()
    {
        Scripted = ReceiptFetchResult.Fetched(DefaultReceipt, "application/pdf");
        LastUrl = null;
        Calls = 0;
    }

    public Task<ReceiptFetchResult> FetchAsync(string receiptUrl, CancellationToken cancellationToken)
    {
        Calls++;
        LastUrl = receiptUrl;
        return Task.FromResult(Scripted);
    }
}
