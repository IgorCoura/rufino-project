namespace BillPayment.Domain.Ports;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Agenda, consulta e cancela um pagamento Pix no provedor — o trilho preferencial (ADR-010).
/// </summary>
/// <remarks>
/// <strong>Este endpoint não documenta idempotência nenhuma</strong>: o <c>externalReference</c>
/// do provedor é campo de busca, não chave de deduplicação (doc 04). A consulta prévia por
/// referência é, portanto, ainda mais obrigatória aqui do que no trilho boleto — sem ela, o
/// trilho que o ADR-010 prefere seria o mais arriscado. Mesmas regras da porta irmã: adapter
/// sem retry, falha modelada, credencial do tenant.
/// </remarks>
public interface IPixPaymentGateway
{
    /// <summary>
    /// Submete o pagamento agendado do QR. <c>value</c> é obrigatório no provedor;
    /// <c>scheduleDate</c> nula = execução imediata (consentida, ADR-017).
    /// </summary>
    Task<PaymentSubmissionResult> PayAsync(
        CredentialRef? credential,
        PixPayload payload,
        Money amount,
        DateOnly? scheduleDate,
        string externalReference,
        string? description,
        CancellationToken cancellationToken);

    Task<PaymentFetchResult> FindByExternalReferenceAsync(
        CredentialRef? credential,
        string externalReference,
        CancellationToken cancellationToken);

    Task<PaymentFetchResult> GetAsync(
        CredentialRef? credential,
        string providerOrderId,
        CancellationToken cancellationToken);

    Task<PaymentCancellationResult> CancelAsync(
        CredentialRef? credential,
        string providerOrderId,
        CancellationToken cancellationToken);
}
