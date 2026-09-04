namespace BillPayment.Application.Queries.Expectations;

/// <summary>Leitura das expectativas do tenant e do painel de pendências.</summary>
public interface IBillExpectationQueries
{
    Task<BillExpectationPage> ListAsync(
        Guid tenantId, string? cursor, int limit, CancellationToken cancellationToken = default);

    Task<BillExpectationDto?> GetAsync(
        Guid tenantId, Guid expectationId, CancellationToken cancellationToken = default);

    /// <param name="dueSoonWindowDays">
    /// Janela do "vence em breve". Sete dias é o padrão porque é o horizonte em que ainda dá
    /// para agir sem encargo.
    /// </param>
    Task<PendingExpectationsView> ListPendingAsync(
        Guid tenantId, int dueSoonWindowDays, CancellationToken cancellationToken = default);
}
