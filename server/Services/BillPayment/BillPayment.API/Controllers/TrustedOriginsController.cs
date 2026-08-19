namespace BillPayment.API.Controllers;

using BillPayment.API.Authorization;
using BillPayment.Application.Mediator;
using BillPayment.Application.Models.TrustedOrigins;
using BillPayment.Application.Queries.TrustedOrigins;
using BillPayment.Application.TrustedOrigins.Commands;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Origens confiáveis do tenant. A ausência de registro significa origem desconhecida —
/// estado válido e comum, por isso <c>resolve</c> devolve 204 em vez de 404.
/// </summary>
/// <remarks>
/// O recurso de autorização se chama <c>origin</c>, no singular e sem o prefixo <c>trusted</c>,
/// para casar com o catálogo do doc 05 — o nome da rota e o nome do recurso não precisam
/// coincidir, e mudá-lo depois exigiria reconfigurar o realm.
/// </remarks>
[Route("api/v1/{tenantId:guid}/trusted-origins")]
public sealed class TrustedOriginsController(
    IMediator mediator,
    ITrustedOriginQueries queries,
    ILogger<TrustedOriginsController> logger) : BaseController(logger)
{
    [HttpGet]
    [ProtectedResource("origin", "view")]
    public async Task<ActionResult<TrustedOriginPage>> List(
        [FromRoute] Guid tenantId,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListAsync(tenantId, cursor, limit, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProtectedResource("origin", "view")]
    public async Task<ActionResult<TrustedOriginDto>> GetById(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var origin = await queries.GetAsync(tenantId, id, cancellationToken);
        return origin is null ? NotFound() : OkResponse(origin);
    }

    /// <summary>Resolve o remetente respeitando a precedência endereço &gt; domínio.</summary>
    [HttpGet("resolve")]
    [ProtectedResource("origin", "view")]
    public async Task<ActionResult<TrustedOriginDto>> Resolve(
        [FromRoute] Guid tenantId,
        [FromQuery] string sender,
        CancellationToken cancellationToken)
    {
        var origin = await queries.ResolveBySenderAsync(tenantId, sender, cancellationToken);

        // Origem desconhecida não é erro — é o caso mais comum antes do usuário decidir.
        return origin is null ? NoContent() : OkResponse(origin);
    }

    [HttpPost]
    [ProtectedResource("origin", "manage")]
    public async Task<ActionResult<RegisterTrustedOriginResponse>> Register(
        [FromRoute] Guid tenantId,
        [FromBody] RegisterTrustedOriginModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId, ResolveDecidingUserId());
        var identified = new IdentifiedCommand<RegisterTrustedOriginCommand, RegisterTrustedOriginResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPut("{id:guid}/decision")]
    [ProtectedResource("origin", "manage")]
    public async Task<ActionResult<ChangeTrustedOriginDecisionResponse>> ChangeDecision(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] ChangeTrustedOriginDecisionModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId, id, ResolveDecidingUserId());
        var identified = new IdentifiedCommand<ChangeTrustedOriginDecisionCommand, ChangeTrustedOriginDecisionResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpDelete("{id:guid}")]
    [ProtectedResource("origin", "manage")]
    public async Task<ActionResult<DeleteTrustedOriginResponse>> Delete(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteTrustedOriginCommand(tenantId, id);
        var identified = new IdentifiedCommand<DeleteTrustedOriginCommand, DeleteTrustedOriginResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }
}
