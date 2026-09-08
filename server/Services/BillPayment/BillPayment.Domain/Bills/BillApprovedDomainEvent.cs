namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Um humano autorizou o pagamento (ADR-007).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Deixou de criar a <c>PaymentOrder</c> em 2026-09-08</strong> (ADR-018). Aprovar e
/// agendar viraram dois atos: este evento diz que alguém autorizou o pagamento, e
/// <see cref="BillSchedulingRequestedDomainEvent"/> diz que alguém escolheu a data e mandou
/// executar. Só o segundo move dinheiro. Este continua sendo a prova de que a autorização
/// humana existiu — sem ele, nenhum agendamento é possível.
/// </para>
/// </remarks>
public sealed record BillApprovedDomainEvent(
    BillId BillId,
    TenantId TenantId,
    UserId ApprovedBy,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}

/// <summary>
/// Um humano escolheu a data e mandou o boleto para a fila de pagamento. É o evento que a fase 3
/// consome para criar a <c>PaymentOrder</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É o único caminho para o dinheiro sair.</strong> Nenhum pagamento existe sem este
/// evento, e nenhum evento destes existe sem uma aprovação humana anterior no mesmo boleto
/// (ADR-007 + ADR-018). O payload carrega a data pedida; a data <em>efetiva</em> é do
/// agendamento, que respeita dia útil e horário de corte — por isso ela não vem aqui.
/// </para>
/// <para>
/// <c>AcknowledgedImmediateExecution</c> é o aceite do ADR-017 <strong>como foi de fato dado</strong>:
/// só é <c>true</c> quando o boleto estava vencido na tela E quem agendou marcou a caixa. O
/// consumidor grava consentimento SÓ com este flag — re-derivar "vencido" no consumo forjaria,
/// num outbox atrasado, um aceite que ninguém deu.
/// </para>
/// </remarks>
public sealed record BillSchedulingRequestedDomainEvent(
    BillId BillId,
    TenantId TenantId,
    UserId RequestedBy,
    DateOnly ScheduleFor,
    bool AcknowledgedImmediateExecution,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}

/// <summary>
/// Uma pessoa desfez uma recusa ou um cancelamento. O boleto voltou à fila de decisão e as
/// verificações vão rodar de novo.
/// </summary>
/// <remarks>
/// A revalidação é consequência obrigatória, não opção de quem reverte: o retrato que sustentava
/// a decisão anterior é velho por definição — entre a recusa e a reversão o documento pode ter
/// sido pago por outro caminho, vencido, ou mudado de valor. Quem reabre não herda um veredito.
/// </remarks>
public sealed record BillDecisionUndoneDomainEvent(
    BillId BillId,
    TenantId TenantId,
    UserId UndoneBy,
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
