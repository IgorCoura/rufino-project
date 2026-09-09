namespace BillPayment.Application.Queries.PayerProfiles;

/// <summary>Um tenant que a varredura de webhook precisa visitar — só o id, nunca o agregado.</summary>
public sealed record WebhookSweepTarget(Guid TenantId);

/// <summary>
/// A fila da varredura de webhooks — separada da query de tela, como as filas irmãs.
/// </summary>
/// <remarks>
/// Não recebe <c>TenantId</c> e <strong>não é travessia</strong>: devolve uma linha por tenant
/// para um processo sem <c>HttpContext</c>, e nada dela responde a um usuário — o mesmo contrato
/// das varreduras de captura, expectativa e conciliação.
/// </remarks>
public interface IPayerProfileWorkQueries
{
    /// <summary>
    /// Os tenants com conta de provedor vinculada — os únicos que podem ter webhook.
    /// </summary>
    /// <remarks>
    /// <strong>Sem claim e sem aluguel, de propósito.</strong> A varredura é de leitura no
    /// provedor e o efeito que ela às vezes dispara é idempotente (o adapter adota o webhook
    /// existente em vez de duplicar); duas réplicas passando pelo mesmo tenant custam uma
    /// chamada a mais, não um webhook a mais. Ordena pelos mais antigos em atualização para que
    /// um lote menor que o total ainda cubra todo mundo em ciclos sucessivos.
    /// </remarks>
    Task<IReadOnlyList<WebhookSweepTarget>> ListWithLinkedAccountAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
