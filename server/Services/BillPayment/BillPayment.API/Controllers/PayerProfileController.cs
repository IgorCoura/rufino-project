namespace BillPayment.API.Controllers;

using BillPayment.API.Authorization;
using BillPayment.Application.Mediator;
using BillPayment.Application.Models.PayerProfiles;
using BillPayment.Application.PayerProfiles.Commands;
using BillPayment.Application.Queries.PayerProfiles;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Cadastro fiscal do tenant. A rota é <strong>singular e sem id</strong> porque existe no
/// máximo um por tenant — expor <c>/{id}</c> sugeriria uma coleção que não existe e daria
/// uma segunda forma de endereçar o mesmo recurso.
/// </summary>
/// <remarks>
/// O cadastro fiscal é o que deriva a senha de PDF e sustenta o degrau 0 do roteamento, então
/// <c>payer-profile:manage</c> não é um cadastro qualquer: mexer nele muda de quem o sistema
/// entende que os boletos são.
/// </remarks>
[Route("api/v1/{tenantId:guid}/payer-profile")]
public sealed class PayerProfileController(
    IMediator mediator,
    IPayerProfileQueries queries,
    ILogger<PayerProfileController> logger) : BaseController(logger)
{
    [HttpGet]
    [ProtectedResource("payer-profile", "view")]
    public async Task<ActionResult<PayerProfileDto>> Get(
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken)
    {
        var profile = await queries.GetByTenantAsync(tenantId, cancellationToken);
        return profile is null ? NotFound() : OkResponse(profile);
    }

    [HttpPost]
    [ProtectedResource("payer-profile", "manage")]
    public async Task<ActionResult<RegisterPayerProfileResponse>> Register(
        [FromRoute] Guid tenantId,
        [FromBody] RegisterPayerProfileModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId);
        var identified = new IdentifiedCommand<RegisterPayerProfileCommand, RegisterPayerProfileResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPut("legal-name")]
    [ProtectedResource("payer-profile", "manage")]
    public async Task<ActionResult<RenamePayerProfileResponse>> Rename(
        [FromRoute] Guid tenantId,
        [FromBody] RenamePayerProfileModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId);
        var identified = new IdentifiedCommand<RenamePayerProfileCommand, RenamePayerProfileResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPost("tax-ids")]
    [ProtectedResource("payer-profile", "manage")]
    public async Task<ActionResult<AddPayerProfileTaxIdResponse>> AddTaxId(
        [FromRoute] Guid tenantId,
        [FromBody] PayerProfileTaxIdModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToAddCommand(tenantId);
        var identified = new IdentifiedCommand<AddPayerProfileTaxIdCommand, AddPayerProfileTaxIdResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// O documento vai na query, e não no path, porque CNPJ formatado contém <c>/</c> e
    /// viraria outro segmento de rota.
    /// </summary>
    [HttpDelete("tax-ids")]
    [ProtectedResource("payer-profile", "manage")]
    public async Task<ActionResult<RemovePayerProfileTaxIdResponse>> RemoveTaxId(
        [FromRoute] Guid tenantId,
        [FromQuery] string taxId,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new RemovePayerProfileTaxIdCommand(tenantId, taxId);
        var identified = new IdentifiedCommand<RemovePayerProfileTaxIdCommand, RemovePayerProfileTaxIdResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPut("cnpj-root-matching")]
    [ProtectedResource("payer-profile", "manage")]
    public async Task<ActionResult<AlterCnpjRootMatchingResponse>> AlterCnpjRootMatching(
        [FromRoute] Guid tenantId,
        [FromBody] AlterCnpjRootMatchingModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId);
        var identified = new IdentifiedCommand<AlterCnpjRootMatchingCommand, AlterCnpjRootMatchingResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPut("asaas-account")]
    [ProtectedResource("payer-profile", "manage")]
    public async Task<ActionResult<LinkAsaasAccountResponse>> LinkAsaasAccount(
        [FromRoute] Guid tenantId,
        [FromBody] LinkAsaasAccountModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId);
        var identified = new IdentifiedCommand<LinkAsaasAccountCommand, LinkAsaasAccountResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }
}
