namespace BillPayment.Application.Queries.TrustedOrigins;

/// <summary>
/// Query side (CQRS). Injetada direto no controller, fora do mediator — é a única
/// exceção autorizada a tocar a Infra, conforme registrado no CLAUDE.md do BC.
/// </summary>
public interface ITrustedOriginQueries
{
    Task<TrustedOriginPage> ListAsync(
        Guid tenantId,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<TrustedOriginDto?> GetAsync(
        Guid tenantId,
        Guid trustedOriginId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve a origem de um remetente. Devolve <c>null</c> quando desconhecida —
    /// que é estado válido, não erro.
    /// </summary>
    Task<TrustedOriginDto?> ResolveBySenderAsync(
        Guid tenantId,
        string senderAddress,
        CancellationToken cancellationToken = default);
}
