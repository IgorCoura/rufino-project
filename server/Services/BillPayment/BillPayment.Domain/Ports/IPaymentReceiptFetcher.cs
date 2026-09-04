namespace BillPayment.Domain.Ports;

/// <summary>
/// Baixa o comprovante de um pagamento a partir da URL que o provedor devolve.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O arquivo é a evidência, não a URL</strong>: a URL do comprovante é credencial ao
/// portador e pode expirar — o que fica guardado é o arquivo no balde, sob a chave do tenant, e
/// o que atravessa esta porta nunca entra em log (só o host, como toda URL de documento do BC).
/// </para>
/// <para>
/// Falha modelada, como as irmãs: "o provedor respondeu que não há comprovante" (permanente) e
/// "não houve resposta" (retentável — a reentrega do outbox tenta de novo) exigem tratamentos
/// opostos.
/// </para>
/// </remarks>
public interface IPaymentReceiptFetcher
{
    Task<ReceiptFetchResult> FetchAsync(string receiptUrl, CancellationToken cancellationToken);
}

/// <summary>O desfecho da busca do comprovante.</summary>
public sealed record ReceiptFetchResult(
    ReadOnlyMemory<byte>? Content,
    string? ContentType,
    string? ReasonCode,
    bool IsRetryable)
{
    public bool IsFetched => Content is not null;

    public static ReceiptFetchResult Fetched(ReadOnlyMemory<byte> content, string? contentType)
        => new(content, contentType, null, IsRetryable: false);

    /// <summary>O provedor respondeu que não há (ou não há mais) comprovante ali. Permanente.</summary>
    public static ReceiptFetchResult NotFound(string reasonCode)
        => new(null, null, reasonCode, IsRetryable: false);

    public static ReceiptFetchResult Unavailable(string reasonCode)
        => new(null, null, reasonCode, IsRetryable: true);
}
