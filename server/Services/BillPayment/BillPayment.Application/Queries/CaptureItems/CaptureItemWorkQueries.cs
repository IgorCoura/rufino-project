namespace BillPayment.Application.Queries.CaptureItems;

using BillPayment.Domain.CaptureItems;
using BillPayment.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

internal sealed class CaptureItemWorkQueries(BillPaymentDbContext context, TimeProvider clock)
    : ICaptureItemWorkQueries
{
    public Task<IReadOnlyList<PendingCaptureItem>> ClaimPendingAsync(
        int limit,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken = default)
        // LinkFailed fica de fora de propósito: a nova tentativa de um download que falhou é
        // decisão de quem opera, não um laço automático que insistiria para sempre contra um
        // anexo que o provedor não entrega.
        => ClaimAsync(CaptureItemStatus.Received, limit, leaseUntil, cancellationToken);

    public Task<IReadOnlyList<PendingCaptureItem>> ClaimPendingVisionAsync(
        int limit,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken = default)
        // Mesma ordem da fila rápida: o mais antigo primeiro. A cota de IA é escassa, e gastá-la
        // com o que chegou agora deixaria o de ontem esperando indefinidamente.
        => ClaimAsync(CaptureItemStatus.VisionPending, limit, leaseUntil, cancellationToken);

    /// <summary>
    /// Escolhe e reserva o lote num único comando, para que ninguém escolha o que já foi escolhido.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong><c>FOR UPDATE SKIP LOCKED</c> é o que torna a reserva segura sob concorrência</strong>:
    /// cada worker tranca as linhas que pegou e os demais pulam essas em vez de esperar por elas.
    /// É o mesmo mecanismo do <c>OutboxProcessor</c> — a fila de captura era a única do BC sem ele.
    /// </para>
    /// <para>
    /// <strong>O <c>UPDATE</c> envolve o <c>SELECT</c> porque o aluguel precisa sobreviver à
    /// transação.</strong> No outbox a trava dura o processamento inteiro, que cabe numa
    /// transação; aqui o processamento leva segundos, baixa arquivo e chama serviço externo —
    /// segurar uma transação de banco por todo esse tempo prenderia conexão à toa. Gravar
    /// <c>lease_expires_at</c> transfere a exclusão da trava do banco para um prazo em coluna, e
    /// esse prazo vence sozinho quando o worker morre.
    /// </para>
    /// <para>
    /// <strong>A condição do aluguel também é o backoff.</strong> Depois de uma falha transitória
    /// o agregado empurra <c>lease_expires_at</c> para o futuro, e este mesmo <c>WHERE</c> passa a
    /// pular o item até lá — sem agendador à parte e sem uma segunda noção de "quando voltar".
    /// </para>
    /// </remarks>
    private async Task<IReadOnlyList<PendingCaptureItem>> ClaimAsync(
        CaptureItemStatus status,
        int limit,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken)
    {
        if (limit <= 0)
            return [];

        var schema = BillPaymentDbContext.DEFAULT_SCHEMA;

        // Só o nome do schema é interpolado, e ele é constante de compilação; todo valor vindo
        // de fora entra parametrizado ({0}..{3}).
        var sql =
            $"UPDATE {schema}.capture_items SET "
            + "lease_expires_at = {0}, processing_attempts = processing_attempts + 1, updated_at = {1} "
            + "WHERE id IN ("
            + $"SELECT id FROM {schema}.capture_items "
            + "WHERE status = {2} AND (lease_expires_at IS NULL OR lease_expires_at <= {1}) "
            + "ORDER BY received_at, id LIMIT {3} FOR UPDATE SKIP LOCKED) "
            // xmin é o token de concorrência do agregado (coluna de sistema): RETURNING * não a
            // devolve, e o EF materializa o item esperando esse campo a mais.
            + "RETURNING *, xmin";

        var now = clock.GetUtcNow().UtcDateTime;

        var claimed = await context.CaptureItems
            .FromSqlRaw(sql, leaseUntil.UtcDateTime, now, status.Id, limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return [.. claimed
            .OrderBy(i => i.ReceivedAt)
            .ThenBy(i => i.Id)
            .Select(i => new PendingCaptureItem(i.TenantId.Value, i.Id.Value))];
    }
}
