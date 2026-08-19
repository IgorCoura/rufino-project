namespace BillPayment.Application.Queries.Bills;

/// <summary>
/// Query side (CQRS). Injetada direto no controller, fora do mediator — é a única
/// exceção autorizada a tocar a Infra, conforme registrado no CLAUDE.md do BC.
/// </summary>
public interface IBillQueries
{
    Task<BillPage> ListAsync(
        Guid tenantId,
        string? status,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<BillDto?> GetAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// O detalhe que a tela de aprovação consome: o boleto, o beneficiário que a consulta
    /// devolveu, e as doze verificações com evidência.
    /// </summary>
    Task<BillDetailDto?> GetDetailAsync(
        Guid tenantId,
        Guid billId,
        CancellationToken cancellationToken = default);
}
