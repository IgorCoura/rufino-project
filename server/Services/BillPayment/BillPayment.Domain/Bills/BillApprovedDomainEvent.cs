namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Um humano autorizou o pagamento. É o evento que a fase 3 consome para criar a
/// <c>PaymentOrder</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É o único caminho para o dinheiro sair.</strong> Nenhum pagamento existe sem este
/// evento, e nenhum evento destes existe sem um <c>UserId</c> (ADR-007). O payload carrega a
/// data pedida; a data <em>efetiva</em> é do agendamento, que respeita dia útil e horário de
/// corte — por isso ela não vem aqui.
/// </para>
/// <para>
/// <c>AcknowledgedImmediateExecution</c> é o aceite do ADR-017 <strong>como foi de fato dado</strong>:
/// só é <c>true</c> quando o boleto estava vencido na tela E o aprovador marcou a caixa. O
/// consumidor grava consentimento SÓ com este flag — re-derivar "vencido" no consumo forjaria,
/// num outbox atrasado, um aceite que ninguém deu.
/// </para>
/// </remarks>
public sealed record BillApprovedDomainEvent(
    BillId BillId,
    TenantId TenantId,
    UserId ApprovedBy,
    DateOnly ScheduleFor,
    bool AcknowledgedImmediateExecution,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}

/// <summary>O humano recusou o boleto. Alimenta a trilha de auditoria e o relatório de exceção.</summary>
public sealed record BillDeniedDomainEvent(
    BillId BillId,
    TenantId TenantId,
    UserId DeniedBy,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}

/// <summary>
/// O boleto saiu do fluxo. Na fase 3 este evento cancela a <c>PaymentOrder</c> se ela já existir.
/// </summary>
public sealed record BillCancelledDomainEvent(
    BillId BillId,
    TenantId TenantId,
    UserId CancelledBy,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}
