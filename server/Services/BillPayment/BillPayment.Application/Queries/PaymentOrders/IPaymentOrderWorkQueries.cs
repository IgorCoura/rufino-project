namespace BillPayment.Application.Queries.PaymentOrders;

/// <summary>Uma ordem reivindicada pela fila de submissão — só os ids, nunca o agregado.</summary>
public sealed record PendingPaymentSubmission(Guid TenantId, Guid PaymentOrderId);

/// <summary>Uma ordem retida por falta de conta de pagamento, para a varredura reconferir.</summary>
public sealed record AccountHeldPaymentOrder(Guid TenantId, Guid PaymentOrderId);

/// <summary>
/// A fila do worker de submissão — separada da query de tela, como as filas irmãs.
/// </summary>
/// <remarks>
/// As varreduras não recebem <c>TenantId</c> e <strong>não são travessia</strong>: devolvem
/// <c>(TenantId, Id)</c> por linha para um processo sem <c>HttpContext</c>, e nada delas
/// responde a um usuário — o mesmo contrato das varreduras de captura e expectativa.
/// </remarks>
public interface IPaymentOrderWorkQueries
{
    /// <summary>
    /// Escolhe e reserva o lote num único comando: <c>Draft</c>, sem retenção, com o aluguel
    /// vencido ou livre. O aluguel também é o backoff — a mesma coluna, a mesma pergunta.
    /// </summary>
    Task<IReadOnlyList<PendingPaymentSubmission>> ClaimPendingSubmissionsAsync(
        int limit,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// As ordens paradas em <c>AwaitingAccount</c>, para reconferir se o tenant já vinculou a
    /// chave. Sem lock: quem muda estado é o comando de liberação, protegido pelo <c>xmin</c>.
    /// </summary>
    Task<IReadOnlyList<AccountHeldPaymentOrder>> ListAccountHeldAsync(
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// As ordens que esperam desfecho do provedor sem notícia há tempo demais — o alvo da
    /// conciliação por polling. <strong>Carimba <c>sweep_attempted_at</c> na saída</strong> e
    /// ordena nunca-tentadas primeiro: é o anti-inanição — ordem que falha conciliação
    /// repetidamente vai para o fim da fila em vez de monopolizar o lote.
    /// </summary>
    Task<IReadOnlyList<PendingPaymentSubmission>> ClaimStaleAwaitingProviderAsync(
        DateTimeOffset syncedBefore,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A rede de segurança do comprovante: ordens pagas sem arquivo no balde e sem a marca de
    /// "sem comprovante", já envelhecidas (o caminho do outbox teve a vez dele). Mesmo carimbo
    /// e mesma ordenação do claim da conciliação — os status são disjuntos, a coluna é uma só.
    /// </summary>
    Task<IReadOnlyList<PendingPaymentSubmission>> ClaimPaidMissingReceiptAsync(
        DateTimeOffset agedBefore,
        int limit,
        CancellationToken cancellationToken = default);
}
