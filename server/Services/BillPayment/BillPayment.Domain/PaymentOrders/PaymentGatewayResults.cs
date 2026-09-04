namespace BillPayment.Domain.PaymentOrders;

/// <summary>
/// O que voltou de uma submissão de pagamento: o retrato aceito, ou o motivo de não haver.
/// </summary>
/// <remarks>
/// <para>
/// Falha é <strong>modelada, nunca lançada</strong> — a mesma doutrina de <c>LookupResult</c>,
/// <c>MailboxResult</c> e <c>PaymentAccountProbe</c>, e aqui ela vale dinheiro: colapsar
/// "o provedor recusou esta ordem" (permanente — insistir não muda) com "o provedor não
/// respondeu" (retentável — mas <strong>só depois de conferir por <c>externalReference</c></strong>,
/// porque um timeout pode ter pago) transformaria uma queda de rede em pagamento duplicado ou
/// em ordem morta.
/// </para>
/// </remarks>
public sealed record PaymentSubmissionResult(
    ProviderPaymentSnapshot? Snapshot,
    string? ReasonCode,
    string? ProviderMessage,
    bool IsRetryable)
{
    public bool IsAccepted => Snapshot is not null;

    public static PaymentSubmissionResult Accepted(ProviderPaymentSnapshot snapshot)
        => new(snapshot ?? throw PaymentOrderErrors.SnapshotRequired(), null, null, IsRetryable: false);

    /// <summary>O provedor respondeu e recusou. Retentar mandaria a mesma ordem para a mesma recusa.</summary>
    public static PaymentSubmissionResult Refused(string reasonCode, string? providerMessage)
        => new(null, reasonCode, providerMessage, IsRetryable: false);

    /// <summary>Não houve resposta útil. Nada se sabe — inclusive se a ordem entrou.</summary>
    public static PaymentSubmissionResult Unavailable(string reasonCode, string? providerMessage)
        => new(null, reasonCode, providerMessage, IsRetryable: true);
}

/// <summary>
/// O que voltou de uma busca de ordem no provedor — por id ou por <c>externalReference</c>.
/// </summary>
/// <remarks>
/// <c>NotFound</c> e <c>Unavailable</c> exigem tratamentos opostos na retentativa de submissão:
/// "não achei" autoriza reenviar (o timeout não tinha pago); "não respondi" obriga a esperar,
/// porque reenviar sem saber é exatamente o pagamento duplicado que a busca existe para impedir.
/// </remarks>
public sealed record PaymentFetchResult(
    ProviderPaymentSnapshot? Snapshot,
    string? ReasonCode,
    bool IsUnavailable)
{
    public bool IsFound => Snapshot is not null;

    public static PaymentFetchResult Found(ProviderPaymentSnapshot snapshot)
        => new(snapshot ?? throw PaymentOrderErrors.SnapshotRequired(), null, IsUnavailable: false);

    public static PaymentFetchResult NotFound()
        => new(null, null, IsUnavailable: false);

    public static PaymentFetchResult Unavailable(string reasonCode)
        => new(null, reasonCode, IsUnavailable: true);
}

/// <summary>O que voltou de um pedido de cancelamento no provedor.</summary>
public sealed record PaymentCancellationResult(
    bool IsCancelled,
    string? ReasonCode,
    bool IsRetryable)
{
    public static PaymentCancellationResult Cancelled()
        => new(true, null, IsRetryable: false);

    /// <summary>O provedor respondeu que não cancela — a ordem já anda. Permanente.</summary>
    public static PaymentCancellationResult Refused(string reasonCode)
        => new(false, reasonCode, IsRetryable: false);

    public static PaymentCancellationResult Unavailable(string reasonCode)
        => new(false, reasonCode, IsRetryable: true);
}
