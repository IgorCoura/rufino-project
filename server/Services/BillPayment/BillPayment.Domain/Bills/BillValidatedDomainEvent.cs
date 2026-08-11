namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// A verificação rodou e nenhuma falha bloqueante apareceu: o boleto está esperando um humano.
/// </summary>
/// <remarks>
/// Consumido pela notificação ao aprovador (fase 4) e pelo casamento com a expectativa aberta
/// (fase 2). <c>AttentionItems</c> viaja no payload porque muda o texto do aviso — "conta nova
/// para aprovar" e "conta para aprovar, com 3 pontos de atenção" são mensagens diferentes.
/// </remarks>
public sealed record BillValidatedDomainEvent(
    BillId BillId,
    TenantId TenantId,
    int AttentionItems,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}

/// <summary>
/// A verificação encontrou falha bloqueante. Alimenta a fila de exceção operacional.
/// </summary>
/// <remarks>
/// <c>ReasonCodes</c> carrega os motivos das falhas bloqueantes — não a evidência, que pode
/// conter nome de beneficiário e valor. Evento vai para o outbox e para o log; quem precisar
/// do detalhe carrega o agregado.
/// </remarks>
public sealed record BillRejectedDomainEvent(
    BillId BillId,
    TenantId TenantId,
    IReadOnlyCollection<string> ReasonCodes,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}
