namespace BillPayment.Domain.SeedWork;

/// <summary>
/// A idempotência do webhook de pagamento: um evento do provedor é processado uma vez, por id.
/// </summary>
/// <remarks>
/// <para>
/// Mora no SeedWork pelo mesmo motivo do <see cref="IRequestManager"/>: é plataforma que a
/// Infra implementa, e <c>Infra → Application</c> seria ciclo.
/// </para>
/// <para>
/// <strong><see cref="RecordAsync"/> não commita</strong> — registra a marca no contexto de quem
/// chamou, e a marca persiste junto com o efeito no <c>SaveEntitiesAsync</c>. Duas entregas
/// concorrentes do mesmo evento colidem na PK, a perdedora estoura, o provedor reentrega, e a
/// releitura encontra a marca — a mesma coreografia do <c>RequestManager</c>.
/// </para>
/// </remarks>
public interface IPaymentWebhookLedger
{
    Task<bool> ExistsAsync(string eventId, CancellationToken cancellationToken = default);

    Task RecordAsync(string eventId, DateTime receivedAt, CancellationToken cancellationToken = default);
}
