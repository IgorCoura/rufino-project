namespace BillPayment.API.Controllers;

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
/// Autorização granular (<c>[ProtectedResource("origin", "view"|"manage")]</c>) entra na
/// fase 6, junto com o Keycloak. Até lá os endpoints estão abertos — ver "Checklist
/// pré-produção" no CLAUDE.md do BC.
/// </remarks>
[Route("api/v1/{tenantId:guid}/trusted-origins")]
public sealed class TrustedOriginsController(
    IMediator mediator,
    ITrustedOriginQueries queries,
    ILogger<TrustedOriginsController> logger) : BaseController(logger)
{
    [HttpGet]
    public async Task<ActionResult<TrustedOriginPage>> List(
        [FromRoute] Guid tenantId,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListAsync(tenantId, cursor, limit, cancellationToken));

    [HttpGet("{id:guid}")]
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
    public async Task<ActionResult<RegisterTrustedOriginResponse>> Register(
        [FromRoute] Guid tenantId,
        [FromBody] RegisterTrustedOriginModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId);
        var identified = new IdentifiedCommand<RegisterTrustedOriginCommand, RegisterTrustedOriginResponse>(
            command, EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }

    [HttpPut("{id:guid}/decision")]
    public async Task<ActionResult<ChangeTrustedOriginDecisionResponse>> ChangeDecision(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] ChangeTrustedOriginDecisionModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId, id);
        var identified = new IdentifiedCommand<ChangeTrustedOriginDecisionCommand, ChangeTrustedOriginDecisionResponse>(
            command, EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<DeleteTrustedOriginResponse>> Delete(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<DeleteTrustedOriginCommand, DeleteTrustedOriginResponse>(
            new DeleteTrustedOriginCommand(tenantId, id), EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }
}
