namespace BillPayment.API.Controllers;

using BillPayment.API.Authorization;
using BillPayment.API.Extension;
using BillPayment.Application.Mediator;
using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Application.Queries.PaymentOrders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// As ordens de pagamento — a execução do que o aprovador autorizou (ADR-002).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Reusa os escopos de <c>bill</c>, de propósito</strong> — a mesma doutrina de
/// <c>capture-item:claim</c> servindo à reprovação: ver a execução é ver o boleto
/// (<c>bill:view</c>); cancelar a execução é a mesma decisão de <c>bill:cancel</c>; e confirmar
/// pagamento imediato é poder de quem aprova (<c>bill:approve</c>). Escopo novo exigiria
/// partial import no realm, que já é pendência de deploy — e nenhuma das três ações cria um
/// poder que os escopos existentes não descrevem.
/// </para>
/// <para>
/// Não há <c>POST</c> de criação: ordem nasce da aprovação, pelo outbox (UC-13 — "sem endpoint
/// próprio: consequência de UC-05").
/// </para>
/// </remarks>
[Route("api/v1/{tenantId:guid}/payments")]
public sealed class PaymentsController(
    IMediator mediator,
    IPaymentQueries queries,
    ILogger<PaymentsController> logger) : BaseController(logger)
{
    [HttpGet]
    [ProtectedResource("bill", "view")]
    public async Task<ActionResult<PaymentOrderPage>> List(
        [FromRoute] Guid tenantId,
        [FromQuery] string? status,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListAsync(tenantId, status, cursor, limit, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProtectedResource("bill", "view")]
    public async Task<ActionResult<PaymentOrderDto>> GetById(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var order = await queries.GetAsync(tenantId, id, cancellationToken);
        return order is null ? NotFound() : OkResponse(order);
    }

    /// <summary>A ordem (mais recente) de um boleto — é o que o detalhe do boleto mostra.</summary>
    [HttpGet("by-bill/{billId:guid}")]
    [ProtectedResource("bill", "view")]
    public async Task<ActionResult<PaymentOrderDto>> GetByBill(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid billId,
        CancellationToken cancellationToken)
    {
        var order = await queries.GetByBillAsync(tenantId, billId, cancellationToken);
        return order is null ? NotFound() : OkResponse(order);
    }

    /// <summary>
    /// Cancela a ordem — a janela de reação da política das 24h (ADR-017). Depois da submissão
    /// o provedor decide se ainda dá; recusa dele sai como 409 com o motivo.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [EnableRateLimiting(RateLimitingExtensions.EXPENSIVE_POLICY)]
    [ProtectedResource("bill", "cancel")]
    public async Task<ActionResult<CancelPaymentOrderResponse>> Cancel(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new CancelPaymentOrderCommand(tenantId, id, ResolveDecidingUserId());
        var identified = new IdentifiedCommand<CancelPaymentOrderCommand, CancelPaymentOrderResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Confirma o pagamento imediato de uma ordem retida — o boleto venceu antes da submissão
    /// e conta vencida é processada na hora, sem janela de reação (ADR-017). Quem confirma vem
    /// do token e fica na trilha (ADR-007).
    /// </summary>
    [HttpPost("{id:guid}/confirm-immediate")]
    [ProtectedResource("bill", "approve")]
    public async Task<ActionResult<ConfirmImmediatePaymentResponse>> ConfirmImmediate(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmImmediatePaymentCommand(tenantId, id, ResolveDecidingUserId());
        var identified = new IdentifiedCommand<ConfirmImmediatePaymentCommand, ConfirmImmediatePaymentResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }
}
