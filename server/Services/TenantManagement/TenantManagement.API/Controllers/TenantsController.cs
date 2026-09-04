namespace TenantManagement.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using TenantManagement.API.Authorization;
using TenantManagement.Application.Mediator;
using TenantManagement.Application.Models.Tenants;
using TenantManagement.Application.Queries.Tenants;
using TenantManagement.Application.Tenants.Commands;

/// <summary>
/// Back-office do cadastro de tenants — pessoa física e jurídica no mesmo lugar.
/// </summary>
/// <remarks>
/// O parâmetro de rota se chama <c>id</c>, e não <c>tenantId</c>, <strong>de propósito</strong>:
/// o guard anti-IDOR de rota casa pelo nome do parâmetro, e um operador da plataforma não tem
/// tenant nenhum no claim. Quem decide o acesso aqui é a policy do realm, via
/// <c>[ProtectedResource]</c>. Renomear este parâmetro tranca o back-office para fora.
/// </remarks>
[Route("api/v1/tenants")]
public sealed class TenantsController(
    IMediator mediator,
    ITenantQueries queries,
    ILogger<TenantsController> logger) : BaseController(logger)
{
    [HttpPost]
    [ProtectedResource("tenant", "create")]
    public async Task<ActionResult<RegisterTenantResponse>> Register(
        [FromBody] RegisterTenantModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        var command = model.ToCommand();
        var identified = new IdentifiedCommand<RegisterTenantCommand, RegisterTenantResponse>(
            command, EnsureRequestId(requestId));

        // Sem id de rota: o tenant ainda vai nascer. O id novo sai no {@Result}.
        SendingCommandLog(null, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, null, command, identified.Id);

        return OkResponse(result);
    }

    [HttpGet]
    [ProtectedResource("tenant", "view")]
    public async Task<ActionResult<TenantPage>> List(
        [FromQuery] string? kind,
        [FromQuery] string? status,
        [FromQuery] string? product,
        [FromQuery] string? search,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListAsync(
            new TenantListFilter(kind, status, product, search), cursor, limit, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProtectedResource("tenant", "view")]
    public async Task<ActionResult<TenantDto>> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var tenant = await queries.GetAsync(id, cancellationToken);
        return tenant is null ? NotFound() : OkResponse(tenant);
    }

    [HttpPut("{id:guid}")]
    [ProtectedResource("tenant", "edit")]
    public async Task<ActionResult<EditTenantDetailsResponse>> Edit(
        [FromRoute] Guid id,
        [FromBody] EditTenantDetailsModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        var command = model.ToCommand(id);
        var identified = new IdentifiedCommand<EditTenantDetailsCommand, EditTenantDetailsResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPut("{id:guid}/address")]
    [ProtectedResource("tenant", "edit")]
    public async Task<ActionResult<ChangeTenantAddressResponse>> ChangeAddress(
        [FromRoute] Guid id,
        [FromBody] ChangeTenantAddressModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        var command = model.ToCommand(id);
        var identified = new IdentifiedCommand<ChangeTenantAddressCommand, ChangeTenantAddressResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPut("{id:guid}/contact")]
    [ProtectedResource("tenant", "edit")]
    public async Task<ActionResult<ChangeTenantContactResponse>> ChangeContact(
        [FromRoute] Guid id,
        [FromBody] ChangeTenantContactModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        var command = model.ToCommand(id);
        var identified = new IdentifiedCommand<ChangeTenantContactCommand, ChangeTenantContactResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPost("{id:guid}/suspend")]
    [ProtectedResource("tenant", "suspend")]
    public async Task<ActionResult<SuspendTenantResponse>> Suspend(
        [FromRoute] Guid id,
        [FromBody] SuspendTenantModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        var command = model.ToCommand(id);
        var identified = new IdentifiedCommand<SuspendTenantCommand, SuspendTenantResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPost("{id:guid}/reactivate")]
    [ProtectedResource("tenant", "suspend")]
    public async Task<ActionResult<ReactivateTenantResponse>> Reactivate(
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new ReactivateTenantCommand(id);
        var identified = new IdentifiedCommand<ReactivateTenantCommand, ReactivateTenantResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPost("{id:guid}/products/{product}")]
    [ProtectedResource("tenant-product", "edit")]
    public async Task<ActionResult<TenantProductResponse>> ActivateProduct(
        [FromRoute] Guid id,
        [FromRoute] string product,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new ActivateTenantProductCommand(id, product);
        var identified = new IdentifiedCommand<ActivateTenantProductCommand, TenantProductResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpDelete("{id:guid}/products/{product}")]
    [ProtectedResource("tenant-product", "edit")]
    public async Task<ActionResult<TenantProductResponse>> DeactivateProduct(
        [FromRoute] Guid id,
        [FromRoute] string product,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateTenantProductCommand(id, product);
        var identified = new IdentifiedCommand<DeactivateTenantProductCommand, TenantProductResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPost("{id:guid}/members")]
    [ProtectedResource("tenant-access", "edit")]
    public async Task<ActionResult<TenantMembershipResponse>> GrantMembership(
        [FromRoute] Guid id,
        [FromBody] GrantTenantMembershipModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        var command = model.ToCommand(id);
        var identified = new IdentifiedCommand<GrantTenantMembershipCommand, TenantMembershipResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// O e-mail vai na query, e não no path, porque endereço contém <c>.</c> e <c>@</c> e o
    /// roteamento do ASP.NET trataria o sufixo como extensão de arquivo.
    /// </summary>
    [HttpDelete("{id:guid}/members")]
    [ProtectedResource("tenant-access", "edit")]
    public async Task<ActionResult<TenantMembershipResponse>> RevokeMembership(
        [FromRoute] Guid id,
        [FromQuery] string email,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new RevokeTenantMembershipCommand(id, email);
        var identified = new IdentifiedCommand<RevokeTenantMembershipCommand, TenantMembershipResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Reenvia ao provedor de identidade o que ficou pendente. É o conserto do único ponto do
    /// BC que não é transacional, e é idempotente: reexecutar não faz estrago.
    /// </summary>
    [HttpPost("{id:guid}/access/reprovision")]
    [ProtectedResource("tenant-access", "edit")]
    public async Task<ActionResult<ReprovisionTenantAccessResponse>> ReprovisionAccess(
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new ReprovisionTenantAccessCommand(id);
        var identified = new IdentifiedCommand<ReprovisionTenantAccessCommand, ReprovisionTenantAccessResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }
}
