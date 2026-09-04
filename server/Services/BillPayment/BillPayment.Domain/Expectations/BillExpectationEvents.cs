namespace BillPayment.Domain.Expectations;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O sistema passou a monitorar uma conta por conta própria.
/// </summary>
/// <remarks>
/// Existe para <strong>notificar</strong>. Criar a expectativa em silêncio seria pior que não
/// criá-la: a primeira notícia da existência dela seria um alerta que o usuário não pediu.
/// </remarks>
public sealed record BillExpectationLearnedDomainEvent(
    BillExpectationId ExpectationId,
    TenantId TenantId,
    string Label,
    string Recurrence,
    DateTime OccurredAt) : IDomainEvent
{
    /// <summary>
    /// Identidade da mensagem, não do fato — é o que permite ao consumidor detectar reentrega.
    /// O outbox garante ao-menos-uma-vez, então o handler precisa ser idempotente.
    /// </summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}


public sealed record BillExpectationCycleOpenedDomainEvent(
    BillExpectationId ExpectationId,
    TenantId TenantId,
    ExpectationCycleId CycleId,
    string Competence,
    DateOnly ExpectedDueDate,
    DateTime OccurredAt) : IDomainEvent
{
    /// <summary>
    /// Identidade da mensagem, não do fato — é o que permite ao consumidor detectar reentrega.
    /// O outbox garante ao-menos-uma-vez, então o handler precisa ser idempotente.
    /// </summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}


public sealed record BillExpectationFulfilledDomainEvent(
    BillExpectationId ExpectationId,
    TenantId TenantId,
    ExpectationCycleId CycleId,
    BillId BillId,
    DateTime OccurredAt) : IDomainEvent
{
    /// <summary>
    /// Identidade da mensagem, não do fato — é o que permite ao consumidor detectar reentrega.
    /// O outbox garante ao-menos-uma-vez, então o handler precisa ser idempotente.
    /// </summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}


/// <summary>A conta não chegou. O alerta manda buscar.</summary>
public sealed record BillExpectationMissedDomainEvent(
    BillExpectationId ExpectationId,
    TenantId TenantId,
    ExpectationCycleId CycleId,
    string Reason,
    DateTime OccurredAt) : IDomainEvent
{
    /// <summary>
    /// Identidade da mensagem, não do fato — é o que permite ao consumidor detectar reentrega.
    /// O outbox garante ao-menos-uma-vez, então o handler precisa ser idempotente.
    /// </summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}


/// <summary>
/// A conta chegou e não deu para ler. O alerta leva direto ao item resolvível — é o mais valioso
/// dos dois, porque o sistema já tem o documento e sabe exatamente o que falta.
/// </summary>
public sealed record BillExpectationCaptureFailedDomainEvent(
    BillExpectationId ExpectationId,
    TenantId TenantId,
    ExpectationCycleId CycleId,
    CaptureItemId CaptureItemId,
    string Reason,
    DateTime OccurredAt) : IDomainEvent
{
    /// <summary>
    /// Identidade da mensagem, não do fato — é o que permite ao consumidor detectar reentrega.
    /// O outbox garante ao-menos-uma-vez, então o handler precisa ser idempotente.
    /// </summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}



/// <summary>
/// Um nível do escalonamento saiu — e saiu uma vez só.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É este o evento que notifica, e não o de "não cumprido".</strong> A transição para
/// <c>Missing</c> acontece uma vez por ciclo; o escalonamento acontece quatro. Enquanto o aviso
/// pendurou no primeiro, os níveis <c>Warning</c>, <c>Urgent</c> e <c>Overdue</c> ficavam
/// gravados no agregado e nunca chegavam a ninguém — a tabela de escalonamento do doc 11 existia
/// só no papel.
/// </para>
/// <para>
/// <paramref name="Arrived"/> é o que separa os dois avisos: "não chegou, vá buscar" de "chegou e
/// não consegui ler, resolva o item". Vem projetado no evento porque é o consumidor que escolhe o
/// texto, e ele não tem o agregado em mãos.
/// </para>
/// <para>
/// <paramref name="CaptureItemId"/> vai como <c>Guid?</c>, e não como o id tipado, porque o
/// payload atravessa o outbox em JSON e é lido por quem só precisa montar um caminho de tela.
/// </para>
/// </remarks>
public sealed record BillExpectationAlertRaisedDomainEvent(
    BillExpectationId ExpectationId,
    TenantId TenantId,
    ExpectationCycleId CycleId,
    string Label,
    string Level,
    string Competence,
    DateOnly ExpectedDueDate,
    string? MissReason,
    bool Arrived,
    Guid? CaptureItemId,
    DateTime OccurredAt) : IDomainEvent
{
    /// <summary>
    /// Identidade da mensagem, não do fato — é o que permite ao consumidor detectar reentrega.
    /// O outbox garante ao-menos-uma-vez, então o handler precisa ser idempotente.
    /// </summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}
