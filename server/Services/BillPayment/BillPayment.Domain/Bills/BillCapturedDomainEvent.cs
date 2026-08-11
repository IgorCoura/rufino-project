namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O documento entrou e já é legítimo do ponto de vista estrutural — DVs e CRC fecham.
/// Dispara a consulta oficial e a validação.
/// </summary>
/// <remarks>
/// O payload não carrega a linha digitável nem o payload Pix: eventos vão para a tabela de
/// outbox e para o log, e instrumento de pagamento não circula por ali. Quem reagir carrega
/// o agregado pelo <see cref="BillId"/>.
/// </remarks>
public sealed record BillCapturedDomainEvent(
    BillId BillId,
    TenantId TenantId,
    string Kind,
    string Rail,
    DateTime OccurredAt) : IDomainEvent
{
    /// <summary>
    /// Identidade da mensagem, não do fato. É o que permite ao consumidor detectar reentrega
    /// — o outbox garante ao-menos-uma-vez, então o handler precisa ser idempotente.
    /// </summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}
