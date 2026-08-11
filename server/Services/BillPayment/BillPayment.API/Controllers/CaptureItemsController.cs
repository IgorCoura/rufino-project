namespace BillPayment.API.Controllers;

using BillPayment.Application.Queries.CaptureItems;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Itens capturados do tenant, incluindo a quarentena.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Somente leitura nesta sprint.</strong> A reivindicação de um item <c>Unrouted</c>
/// (<c>POST /{id}/claim</c>) entra na 2.6, junto com a escada de roteamento que a torna
/// significativa — ela precisa criar a <c>Bill</c> e a <c>RoutingRule</c> correspondentes.
/// </para>
/// <para>
/// O conteúdo devolvido é filtrado pelo status do item, e quem decide isso é o domínio
/// (<c>CaptureItemStatus.ExposesFinancialDetail</c>), não este controller nem a tela.
/// </para>
/// </remarks>
[Route("api/v1/{tenantId:guid}/capture-items")]
public sealed class CaptureItemsController(
    ICaptureItemQueries queries,
    ILogger<CaptureItemsController> logger) : BaseController(logger)
{
    /// <summary>
    /// Lista os itens capturados. <paramref name="status"/> monta a fila de quarentena —
    /// <c>Unrouted</c> é a fila de reivindicação do dono da fonte.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<CaptureItemPage>> List(
        [FromRoute] Guid tenantId,
        [FromQuery] string? status,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListAsync(tenantId, status, cursor, limit, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CaptureItemDto>> GetById(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var item = await queries.GetAsync(tenantId, id, cancellationToken);
        return item is null ? NotFound() : OkResponse(item);
    }
}
