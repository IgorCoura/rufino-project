namespace BillPayment.API.Controllers;

using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
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
    IMediator mediator,
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

    /// <summary>
    /// Devolve o artefato à fila para a cascata de hoje avaliá-lo de novo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// O desfecho de um item é do dia em que ele passou: a cascata ganha degraus, o prompt muda e
    /// o cadastro muda — sem <c>PayerProfile</c> não há senha derivada, e sem <c>Payee</c> nem
    /// <c>TrustedOrigin</c> o que o parser erra é descartado. Sem este endpoint, reavaliar exigia
    /// apagar linha no banco.
    /// </para>
    /// <para>
    /// <strong>Um por vez, de propósito</strong>: a extração por visão custa por documento e a
    /// conta tem teto diário — reabrir a quarentena inteira queimaria a cota antes de chegar nos
    /// itens que interessam.
    /// </para>
    /// </remarks>
    [HttpPost("{id:guid}/reprocess")]
    public async Task<ActionResult<ReprocessCaptureItemResponse>> Reprocess(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<ReprocessCaptureItemCommand, ReprocessCaptureItemResponse>(
            new ReprocessCaptureItemCommand(tenantId, id), EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }
}
