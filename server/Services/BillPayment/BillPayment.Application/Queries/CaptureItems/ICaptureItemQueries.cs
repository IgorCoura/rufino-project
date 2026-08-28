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

    /// <summary>
    /// O documento original do item, para uma pessoa conferir antes de decidir se reivindica.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Devolve <c>null</c> em toda negativa, e nunca diz qual delas foi.</strong> Item de
    /// outro tenant, item de outro pagador, item sem arquivo guardado e chave órfã saem iguais —
    /// distinguir confirmaria a existência do item, que é a informação que o ADR-008 protege.
    /// </para>
    /// <para>
    /// O gate de visibilidade é o mesmo do <see cref="CaptureItemDto"/>:
    /// <c>CaptureItemStatus.ExposesFinancialDetail</c>. A chave de armazenamento não entra por
    /// parâmetro em hipótese nenhuma — só o id do item.
    /// </para>
    /// <para>
    /// <strong>PDF cifrado sai destravado.</strong> A senha derivada do cadastro é do sistema, e
    /// quem confere o documento não tem como saber o que o emissor usou. A cópia legível nasce a
    /// cada leitura; o original guardado continua cifrado e a senha continua sem sair (ADR-009).
    /// </para>
    /// </remarks>
    Task<ArtifactDownload?> GetArtifactAsync(
        Guid tenantId,
        Guid captureItemId,
        CancellationToken cancellationToken = default);
}
