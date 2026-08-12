namespace BillPayment.API.Controllers;

using BillPayment.Application.Expectations.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Models.Expectations;
using BillPayment.Application.Queries.Expectations;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// O que o tenant espera receber, e o painel do que está pendente.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É a rede de segurança contra falha silenciosa (ADR-014).</strong> Sem DDA nenhum canal
/// garante que a conta foi emitida, então automatizar a captura sem isto aumentaria o risco de
/// esquecimento — trocaria a conferência manual, que ao menos falha de forma visível, por uma
/// automação que falha em silêncio.
/// </para>
/// <para>
/// <strong>O painel é o canal que sempre funciona.</strong> O alerta é registrado no agregado, não
/// no meio de envio, então ele aparece em <c>GET /pending</c> mesmo que nenhum e-mail saia.
/// </para>
/// </remarks>
[Route("api/v1/{tenantId:guid}/expectations")]
public sealed class ExpectationsController(
    IMediator mediator,
    IBillExpectationQueries queries,
    ILogger<ExpectationsController> logger) : BaseController(logger)
{
    [HttpGet]
    public async Task<ActionResult<BillExpectationPage>> List(
        [FromRoute] Guid tenantId,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListAsync(tenantId, cursor, limit, cancellationToken));

    /// <summary>
    /// O que está atrasado, o que chegou e não deu para ler, e o que vence em breve.
    /// </summary>
    /// <remarks>
    /// As três listas vêm separadas porque a ação do usuário muda: a primeira manda buscar, a
    /// segunda leva ao item resolvível em um clique, e a terceira é só antecedência.
    /// </remarks>
    [HttpGet("pending")]
    public async Task<ActionResult<PendingExpectationsView>> Pending(
        [FromRoute] Guid tenantId,
        [FromQuery] int dueSoonWindowDays,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListPendingAsync(tenantId, dueSoonWindowDays, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BillExpectationDto>> GetById(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var expectation = await queries.GetAsync(tenantId, id, cancellationToken);
        return expectation is null ? NotFound() : OkResponse(expectation);
    }

    /// <summary>
    /// Cadastra o que o sistema deve esperar.
    /// </summary>
    /// <remarks>
    /// <strong>É o caminho obrigatório quando o tenant tem mais de uma conta do mesmo
    /// beneficiário</strong> — quatro instalações da EDP, no arquivo medido. A referência de conta
    /// separa uma da outra, e ela é informada porque não há posição fixa no código de barras de
    /// onde deduzi-la.
    /// </remarks>
    [HttpPost]
    public async Task<ActionResult<RegisterBillExpectationResponse>> Register(
        [FromRoute] Guid tenantId,
        [FromBody] RegisterBillExpectationModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<RegisterBillExpectationCommand, RegisterBillExpectationResponse>(
            model.ToCommand(tenantId), EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }

    /// <summary>Pausa, retoma ou desativa o monitoramento.</summary>
    [HttpPut("{id:guid}/watch")]
    public async Task<ActionResult<AlterBillExpectationWatchResponse>> AlterWatch(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] AlterBillExpectationWatchModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<AlterBillExpectationWatchCommand, AlterBillExpectationWatchResponse>(
            model.ToCommand(tenantId, id), EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }

    /// <summary>
    /// "Este mês não vem" — dispensa o ciclo sem desativar a expectativa.
    /// </summary>
    /// <remarks>
    /// É a defesa mais barata contra o falso positivo. Sem ela, a única saída para um mês atípico
    /// seria desativar a expectativa inteira — e ninguém a reativaria depois.
    /// </remarks>
    [HttpPost("{id:guid}/cycles/{cycleId:guid}/waive")]
    public async Task<ActionResult<WaiveExpectationCycleResponse>> WaiveCycle(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromRoute] Guid cycleId,
        [FromBody] WaiveExpectationCycleModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        [FromHeader(Name = "x-user-id")] Guid userId,
        CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<WaiveExpectationCycleCommand, WaiveExpectationCycleResponse>(
            model.ToCommand(tenantId, id, cycleId, ResolveDecidingUserId(userId)),
            EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }
}
