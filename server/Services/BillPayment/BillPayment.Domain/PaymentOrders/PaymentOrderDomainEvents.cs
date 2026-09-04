namespace BillPayment.Domain.PaymentOrders;

using BillPayment.Domain.Bills;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// A ordem foi aceita pelo provedor com a data efetiva calculada. É o evento que leva o
/// <c>Bill</c> de <c>Approved</c> a <c>Scheduled</c> (<c>Bill.LinkPaymentOrder</c>).
/// </summary>
/// <remarks>
/// O <c>Bill</c> é <strong>espelho</strong> da execução (ADR-002): ele só muda de
/// <c>Scheduled</c> em diante por reflexo destes eventos, nunca por escrita direta de handler.
/// </remarks>
public sealed record PaymentOrderScheduledDomainEvent(
    PaymentOrderId PaymentOrderId,
    TenantId TenantId,
    BillId BillId,
    DateOnly EffectiveScheduleDate,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}

/// <summary>O dinheiro saiu. Reflete no <c>Bill</c> e dispara a captura do comprovante.</summary>
public sealed record PaymentOrderPaidDomainEvent(
    PaymentOrderId PaymentOrderId,
    TenantId TenantId,
    BillId BillId,
    DateOnly PaidAt,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}

/// <summary>
/// A execução não fechou — o provedor falhou o pagamento, ou a submissão desistiu. Reflete no
/// <c>Bill</c> (<c>Scheduled → Failed</c>) e alimenta a fila operacional e o alerta.
/// </summary>
public sealed record PaymentOrderFailedDomainEvent(
    PaymentOrderId PaymentOrderId,
    TenantId TenantId,
    BillId BillId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}

/// <summary>A ordem saiu do fluxo antes de executar.</summary>
public sealed record PaymentOrderCancelledDomainEvent(
    PaymentOrderId PaymentOrderId,
    TenantId TenantId,
    BillId BillId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}

/// <summary>
/// A ordem parou em "aguardando confirmação": o boleto venceu entre a aprovação e a submissão,
/// e o ADR-017 proíbe pagar vencido em silêncio. O consumidor avisa o tenant.
/// </summary>
public sealed record PaymentOrderHeldForConfirmationDomainEvent(
    PaymentOrderId PaymentOrderId,
    TenantId TenantId,
    BillId BillId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}

/// <summary>O dinheiro voltou depois de pago. Alimenta a fila operacional — decisão é de gente.</summary>
public sealed record PaymentOrderRefundedDomainEvent(
    PaymentOrderId PaymentOrderId,
    TenantId TenantId,
    BillId BillId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}
