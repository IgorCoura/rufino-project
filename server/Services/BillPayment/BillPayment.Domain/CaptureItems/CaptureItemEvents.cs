namespace BillPayment.Domain.CaptureItems;

using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Um artefato chegou e o sistema não conseguiu concluir sozinho.
/// </summary>
/// <remarks>
/// <para>
/// <strong>São os PRIMEIROS eventos deste agregado, e existem por um motivo só:</strong> ligar a
/// captura à expectativa. O <c>CaptureItem</c> não tem beneficiário nem vencimento — quando ele
/// trava em <c>Locked</c> ou <c>LinkFailed</c>, a falha aconteceu <em>antes</em> da extração —,
/// então a única coisa que o liga a uma conta esperada é a <strong>fonte por onde entrou</strong>.
/// É por isso que <paramref name="SourceId"/> viaja aqui, e por isso a
/// <c>BillExpectation.HintSourceId</c> precisou parar de nascer nula.
/// </para>
/// <para>
/// <strong>Nenhum dado do documento atravessa.</strong> O evento diz que algo chegou por uma
/// fonte e travou; quem decide se aquilo interessa a alguma expectativa é o consumidor, dentro do
/// tenant. Levar valor, beneficiário ou linha digitável aqui furaria o ADR-008 pelo outbox.
/// </para>
/// </remarks>
public sealed record CaptureItemStuckDomainEvent(
    CaptureItemId CaptureItemId,
    TenantId TenantId,
    CaptureSourceId SourceId,
    string Status,
    DateTime ReceivedAt,
    DateTime OccurredAt) : IDomainEvent
{
    /// <summary>
    /// Identidade da mensagem, não do fato — é o que permite ao consumidor detectar reentrega.
    /// O outbox garante ao-menos-uma-vez, então o handler precisa ser idempotente.
    /// </summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}

/// <summary>
/// O artefato que estava preso saiu — virou boleto, foi reprovado, reaberto ou descartado.
/// </summary>
/// <remarks>
/// <strong>Existe para o painel parar de mentir.</strong> Sem ele, um ciclo de expectativa
/// marcado como "chegou e não consegui ler" continuaria apontando para um item que já foi
/// resolvido, e alerta que aponta para trabalho concluído treina a pessoa a ignorar alerta tão
/// bem quanto alerta indevido.
/// </remarks>
public sealed record CaptureItemUnstuckDomainEvent(
    CaptureItemId CaptureItemId,
    TenantId TenantId,
    string Status,
    DateTime OccurredAt) : IDomainEvent
{
    /// <summary>
    /// Identidade da mensagem, não do fato — é o que permite ao consumidor detectar reentrega.
    /// O outbox garante ao-menos-uma-vez, então o handler precisa ser idempotente.
    /// </summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}
