namespace BillPayment.Domain.Ports;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Agenda, consulta e cancela o pagamento de um documento de código de barras no provedor.
/// É a porta pela qual o dinheiro sai — a chamada mais perigosa do BC.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O adapter desta porta NÃO retenta</strong>, ao contrário dos de consulta: uma
/// retentativa de rede numa submissão é candidata a pagamento duplicado. A retentativa é da
/// fila de submissão, e ela <strong>começa por <see cref="FindByExternalReferenceAsync"/></strong>
/// — só reenvia quando o provedor confirma que a referência não existe.
/// </para>
/// <para>
/// Falha é <strong>modelada, nunca lançada</strong> (doutrina do <c>LookupResult</c>): recusa
/// permanente e indisponibilidade retentável exigem tratamentos opostos, e colapsá-las aqui
/// vale dinheiro. A credencial é <strong>do tenant</strong> (ADR-016); nula degrada para
/// <c>Unavailable</c>, nunca usa chave de outro tenant.
/// </para>
/// </remarks>
public interface IBillPaymentGateway
{
    /// <summary>
    /// Submete o agendamento. <c>externalReference</c> é a chave de idempotência.
    /// <c>scheduleDate</c> nula = execução imediata (boleto vencido, com consentimento do
    /// ADR-017 já gravado na ordem) — o provedor processa na hora de qualquer forma.
    /// </summary>
    Task<PaymentSubmissionResult> ScheduleAsync(
        CredentialRef? credential,
        DigitableLine digitableLine,
        Money amount,
        DateOnly? dueDate,
        DateOnly? scheduleDate,
        string externalReference,
        string? description,
        CancellationToken cancellationToken);

    /// <summary>
    /// Procura a ordem pela nossa referência — o passo obrigatório antes de qualquer reenvio.
    /// </summary>
    Task<PaymentFetchResult> FindByExternalReferenceAsync(
        CredentialRef? credential,
        string externalReference,
        CancellationToken cancellationToken);

    /// <summary>O retrato atual da ordem no provedor. Alimenta a conciliação e o comprovante.</summary>
    Task<PaymentFetchResult> GetAsync(
        CredentialRef? credential,
        string providerOrderId,
        CancellationToken cancellationToken);

    Task<PaymentCancellationResult> CancelAsync(
        CredentialRef? credential,
        string providerOrderId,
        CancellationToken cancellationToken);
}
