namespace BillPayment.Application.Queries.CaptureItems;

/// <summary>
/// Leitura da fila de itens capturados do tenant, incluindo a quarentena.
/// </summary>
/// <remarks>
/// <strong>Toda projeção passa por <c>CaptureItemDto.From</c></strong>, que aplica o nível de
/// visibilidade do status (ADR-008). Montar o DTO à mão aqui furaria a regra sem quebrar
/// compilação nem teste — por isso não há outro caminho de construção.
/// </remarks>
public interface ICaptureItemQueries
{
    /// <summary>
    /// Lista os itens do tenant, opcionalmente filtrando por status.
    /// </summary>
    /// <param name="status">
    /// Nome do <c>CaptureItemStatus</c> (ex.: <c>Unrouted</c>) para montar a fila de
    /// reivindicação. Nulo ou desconhecido devolve todos.
    /// </param>
    Task<CaptureItemPage> ListAsync(
        Guid tenantId,
        string? status,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<CaptureItemDto?> GetAsync(Guid tenantId, Guid captureItemId, CancellationToken cancellationToken = default);
}
