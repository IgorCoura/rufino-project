namespace BillPayment.API.Controllers;

using BillPayment.API.Authorization;
using BillPayment.Application.Bills.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Models.Bills;
using BillPayment.Application.Queries.Bills;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Entrada e leitura de documentos de cobrança.
/// </summary>
/// <remarks>
/// <c>bill:approve</c> é a ação sensível deste BC — é ela que autoriza dinheiro sair (ADR-007),
/// e por isso tem escopo próprio em vez de cair num <c>manage</c> junto com o resto. O mesmo
/// vale para <c>deny</c> e <c>cancel</c>: quem só importa boleto não decide o destino dele.
/// </remarks>
[Route("api/v1/{tenantId:guid}/bills")]
public sealed class BillsController(
    IMediator mediator,
    IBillQueries queries,
    ILogger<BillsController> logger) : BaseController(logger)
{
    [HttpGet]
    [ProtectedResource("bill", "view")]
    public async Task<ActionResult<BillPage>> List(
        [FromRoute] Guid tenantId,
        [FromQuery] string? status,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListAsync(tenantId, status, cursor, limit, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProtectedResource("bill", "view")]
    public async Task<ActionResult<BillDto>> GetById(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var bill = await queries.GetAsync(tenantId, id, cancellationToken);
        return bill is null ? NotFound() : OkResponse(bill);
    }

    /// <summary>Importa um documento a partir da linha digitável, do QR Pix, ou dos dois.</summary>
    [HttpPost("import")]
    [ProtectedResource("bill", "import")]
    public async Task<ActionResult<ImportBillResponse>> Import(
        [FromRoute] Guid tenantId,
        [FromBody] ImportBillModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId);
        var identified = new IdentifiedCommand<ImportBillCommand, ImportBillResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>Detalhe com as doze verificações e a evidência de cada uma.</summary>
    [HttpGet("{id:guid}/detail")]
    [ProtectedResource("bill", "view")]
    public async Task<ActionResult<BillDetailDto>> GetDetail(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var detail = await queries.GetDetailAsync(tenantId, id, cancellationToken);
        return detail is null ? NotFound() : OkResponse(detail);
    }

    /// <summary>
    /// Reexecuta a consulta oficial e as verificações. É o botão de revalidar da tela, e o
    /// caminho para o retrato voltar a estar dentro do prazo antes de aprovar.
    /// </summary>
    [HttpPost("{id:guid}/revalidate")]
    [ProtectedResource("bill", "validate")]
    public async Task<ActionResult<ValidateBillResponse>> Revalidate(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new ValidateBillCommand(tenantId, id);
        var identified = new IdentifiedCommand<ValidateBillCommand, ValidateBillResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>Autoriza o pagamento. Nenhum boleto é pago sem passar por aqui (ADR-007).</summary>
    [HttpPost("{id:guid}/approve")]
    [ProtectedResource("bill", "approve")]
    public async Task<ActionResult<ApproveBillResponse>> Approve(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] ApproveBillModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId, id, ResolveDecidingUserId());
        var identified = new IdentifiedCommand<ApproveBillCommand, ApproveBillResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>Recusa o boleto. O motivo é obrigatório.</summary>
    [HttpPost("{id:guid}/deny")]
    [ProtectedResource("bill", "deny")]
    public async Task<ActionResult<DenyBillResponse>> Deny(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] BillDecisionModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToDenyCommand(tenantId, id, ResolveDecidingUserId());
        var identified = new IdentifiedCommand<DenyBillCommand, DenyBillResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>Tira o boleto do fluxo — inclusive um que nem chegou a ser verificado.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProtectedResource("bill", "cancel")]
    public async Task<ActionResult<CancelBillResponse>> Cancel(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] BillDecisionModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCancelCommand(tenantId, id, ResolveDecidingUserId());
        var identified = new IdentifiedCommand<CancelBillCommand, CancelBillResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }
}
