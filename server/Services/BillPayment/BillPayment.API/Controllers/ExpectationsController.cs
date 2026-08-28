namespace BillPayment.API.Controllers;

using BillPayment.API.Authorization;
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
/// <para>
/// <c>waive</c> tem escopo PRÓPRIO pelo que ele faz: dispensar um ciclo é silenciar a rede de
/// segurança para aquela conta. Sob o mesmo escopo de <c>manage</c>, quem cadastra expectativa
/// também poderia apagar o aviso de que a conta não chegou — que é a falha silenciosa que o
/// ADR-014 existe para impedir.
/// </para>
/// </remarks>
[Route("api/v1/{tenantId:guid}/expectations")]
public sealed class ExpectationsController(
    IMediator mediator,
    IBillExpectationQueries queries,
    ILogger<ExpectationsController> logger) : BaseController(logger)
{
    [HttpGet]
    [ProtectedResource("expectation", "view")]
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
    [ProtectedResource("expectation", "view")]
    public async Task<ActionResult<PendingExpectationsView>> Pending(
        [FromRoute] Guid tenantId,
        [FromQuery] int dueSoonWindowDays,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListPendingAsync(tenantId, dueSoonWindowDays, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProtectedResource("expectation", "view")]
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
    [ProtectedResource("expectation", "manage")]
    public async Task<ActionResult<RegisterBillExpectationResponse>> Register(
        [FromRoute] Guid tenantId,
        [FromBody] RegisterBillExpectationModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId);
        var identified = new IdentifiedCommand<RegisterBillExpectationCommand, RegisterBillExpectationResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Corrige o cadastro — tudo menos o beneficiário.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>O beneficiário não é editável, e isso é regra de produto.</strong> Trocá-lo
    /// descreveria outra expectativa, não esta corrigida, e os ciclos já abertos passariam a
    /// esperar uma conta que nunca teve relação com eles. Para trocar, exclua e cadastre de novo.
    /// </para>
    /// <para>
    /// Editar torna a expectativa <c>Manual</c> mesmo que ela tenha nascido do histórico, e
    /// reposiciona os ciclos que <em>ainda esperam</em> — nunca os que já se pronunciaram. Ver
    /// <c>BillExpectation.Reconfigure</c>.
    /// </para>
    /// </remarks>
    [HttpPut("{id:guid}")]
    [ProtectedResource("expectation", "manage")]
    public async Task<ActionResult<EditBillExpectationResponse>> Edit(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] EditBillExpectationModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId, id);
        var identified = new IdentifiedCommand<EditBillExpectationCommand, EditBillExpectationResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Apaga a expectativa e o histórico de ciclos dela.
    /// </summary>
    /// <remarks>
    /// <strong>Excluir não é "nunca mais".</strong> Uma expectativa aprendida pode voltar a ser
    /// aprendida no próximo boleto aprovado daquele beneficiário — é a auto-cura do ADR-014.
    /// Quem quer parar de monitorar de vez desativa por <c>PUT /{id}/watch</c>, que deixa a
    /// decisão registrada.
    /// </remarks>
    [HttpDelete("{id:guid}")]
    [ProtectedResource("expectation", "manage")]
    public async Task<ActionResult<DeleteBillExpectationResponse>> Delete(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteBillExpectationCommand(tenantId, id);
        var identified = new IdentifiedCommand<DeleteBillExpectationCommand, DeleteBillExpectationResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>Pausa, retoma ou desativa o monitoramento.</summary>
    [HttpPut("{id:guid}/watch")]
    [ProtectedResource("expectation", "manage")]
    public async Task<ActionResult<AlterBillExpectationWatchResponse>> AlterWatch(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] AlterBillExpectationWatchModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId, id);
        var identified = new IdentifiedCommand<AlterBillExpectationWatchCommand, AlterBillExpectationWatchResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// "Este mês não vem" — dispensa o ciclo sem desativar a expectativa.
    /// </summary>
    /// <remarks>
    /// É a defesa mais barata contra o falso positivo. Sem ela, a única saída para um mês atípico
    /// seria desativar a expectativa inteira — e ninguém a reativaria depois.
    /// </remarks>
    [HttpPost("{id:guid}/cycles/{cycleId:guid}/waive")]
    [ProtectedResource("expectation", "waive")]
    public async Task<ActionResult<WaiveExpectationCycleResponse>> WaiveCycle(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromRoute] Guid cycleId,
        [FromBody] WaiveExpectationCycleModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId, id, cycleId, ResolveDecidingUserId());
        var identified = new IdentifiedCommand<WaiveExpectationCycleCommand, WaiveExpectationCycleResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }
}
