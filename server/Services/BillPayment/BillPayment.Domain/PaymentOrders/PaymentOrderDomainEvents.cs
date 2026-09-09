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
/// <param name="Origin">
/// <strong>De ONDE partiu o cancelamento</strong>, e é o campo que a trilha do boleto consome.
/// Sem ele, o mesmo evento servia ao cancelamento pedido no nosso app e ao feito no painel do
/// provedor — e o espelho gravava "Sistema" nos dois, deixando sem resposta a pergunta "quem
/// cancelou isto?". Acrescentado em 2026-09-08.
///
/// <strong>Viaja como o NOME, não como o Smart Enum.</strong> O evento atravessa o outbox, que o
/// serializa em JSON e o reconstrói do outro lado — e <c>Enumeration</c> não tem construtor que o
/// desserializador saiba usar. Enquanto o campo foi tipado, TODO cancelamento morria na fila com
/// <c>NotSupportedException</c> e ia para dead-letter: a ordem cancelava e o boleto ficava com a
/// data e o vínculo de um pagamento que não ia acontecer, em silêncio. Quem consome já falava
/// nome (<c>MarkBillScheduleCancelledCommand</c>), então a tradução acontece num lugar só.
/// </param>
/// <param name="RequestedBy">
/// Quem pediu, quando <paramref name="Origin"/> é <c>User</c>. Nulo nas demais origens: atribuir
/// a alguém um ato do provedor seria pior que não ter autor nenhum.
/// </param>
public sealed record PaymentOrderCancelledDomainEvent(
    PaymentOrderId PaymentOrderId,
    TenantId TenantId,
    BillId BillId,
    string Origin,
    UserId? RequestedBy,
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
