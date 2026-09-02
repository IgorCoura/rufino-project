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
}
