namespace BillPayment.API.Controllers;

using BillPayment.Application.Mediator;
using BillPayment.Application.Models.Payees;
using BillPayment.Application.Payees.Commands;
using BillPayment.Application.Queries.Payees;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Beneficiários que o tenant espera pagar. É contra este cadastro que a verificação do
/// boleto compara beneficiário, banco recebedor e valor.
/// </summary>
/// <remarks>
/// Autorização granular (<c>[ProtectedResource("payee", "view"|"manage")]</c>) entra na fase
/// 6, junto com o Keycloak — ver "Checklist pré-produção" no CLAUDE.md do BC.
/// </remarks>
[Route("api/v1/{tenantId:guid}/payees")]
public sealed class PayeesController(
    IMediator mediator,
    IPayeeQueries queries,
    ILogger<PayeesController> logger) : BaseController(logger)
{
    [HttpGet]
    public async Task<ActionResult<PayeePage>> List(
        [FromRoute] Guid tenantId,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListAsync(tenantId, cursor, limit, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PayeeDto>> GetById(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var payee = await queries.GetAsync(tenantId, id, cancellationToken);
        return payee is null ? NotFound() : OkResponse(payee);
    }

    /// <summary>
    /// Busca pelo documento — a chave que a verificação do boleto tem em mãos. O documento
    /// vai na query, e não no path, porque CNPJ formatado contém <c>/</c> e viraria outro
    /// segmento de rota.
    /// </summary>
    [HttpGet("by-tax-id")]
    public async Task<ActionResult<PayeeDto>> GetByTaxId(
        [FromRoute] Guid tenantId,
        [FromQuery] string taxId,
        CancellationToken cancellationToken)
    {
        var payee = await queries.FindByTaxIdAsync(tenantId, taxId, cancellationToken);

        // Beneficiário não cadastrado não é erro — é o estado que torna o check inconclusivo.
        return payee is null ? NoContent() : OkResponse(payee);
    }

    [HttpPost]
    public async Task<ActionResult<RegisterPayeeResponse>> Register(
        [FromRoute] Guid tenantId,
        [FromBody] RegisterPayeeModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId);
        var identified = new IdentifiedCommand<RegisterPayeeCommand, RegisterPayeeResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPut("{id:guid}/legal-name")]
    public async Task<ActionResult<RenamePayeeResponse>> Rename(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] RenamePayeeModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId, id);
        var identified = new IdentifiedCommand<RenamePayeeCommand, RenamePayeeResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPut("{id:guid}/amount-policy")]
    public async Task<ActionResult<AlterPayeeAmountPolicyResponse>> AlterAmountPolicy(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] AlterPayeeAmountPolicyModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId, id);
        var identified = new IdentifiedCommand<AlterPayeeAmountPolicyCommand, AlterPayeeAmountPolicyResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPost("{id:guid}/aliases")]
    public async Task<ActionResult<AddPayeeAliasResponse>> AddAlias(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] PayeeAliasModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToAddCommand(tenantId, id);
        var identified = new IdentifiedCommand<AddPayeeAliasCommand, AddPayeeAliasResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>O apelido vai na query: é texto livre e pode conter barra.</summary>
    [HttpDelete("{id:guid}/aliases")]
    public async Task<ActionResult<RemovePayeeAliasResponse>> RemoveAlias(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromQuery] string alias,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new RemovePayeeAliasCommand(tenantId, id, alias);
        var identified = new IdentifiedCommand<RemovePayeeAliasCommand, RemovePayeeAliasResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPost("{id:guid}/accepted-banks")]
    public async Task<ActionResult<AllowPayeeBankResponse>> AllowBank(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] PayeeBankModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToAllowCommand(tenantId, id);
        var identified = new IdentifiedCommand<AllowPayeeBankCommand, AllowPayeeBankResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpDelete("{id:guid}/accepted-banks/{bankCode}")]
    public async Task<ActionResult<DisallowPayeeBankResponse>> DisallowBank(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromRoute] string bankCode,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new DisallowPayeeBankCommand(tenantId, id, bankCode);
        var identified = new IdentifiedCommand<DisallowPayeeBankCommand, DisallowPayeeBankResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPut("{id:guid}/activation")]
    public async Task<ActionResult<AlterPayeeActivationResponse>> AlterActivation(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] AlterPayeeActivationModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId, id);
        var identified = new IdentifiedCommand<AlterPayeeActivationCommand, AlterPayeeActivationResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<DeletePayeeResponse>> Delete(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new DeletePayeeCommand(tenantId, id);
        var identified = new IdentifiedCommand<DeletePayeeCommand, DeletePayeeResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }
}
