namespace BillPayment.API.Controllers;

using BillPayment.API.Authorization;
using BillPayment.Application.Mediator;
using BillPayment.Application.Models.Retention;
using BillPayment.Application.Queries.Retention;
using BillPayment.Application.Retention.Commands;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Por quanto tempo o histórico de e-mails descartados é guardado — uma política por tenant.
/// </summary>
/// <remarks>
/// <para>
/// Rota singular e sem <c>/{id}</c>, como o <c>payer-profile</c>: é uma por tenant, garantida por
/// índice único.
/// </para>
/// <para>
/// <strong>Ler e alterar têm escopos diferentes.</strong> Quem opera precisa saber qual prazo
/// está em vigor para interpretar a tela; decidir por quanto tempo o histórico existe é decisão
/// de instalação, e por isso <c>manage</c> fica com o administrador.
/// </para>
/// </remarks>
[Route("api/v1/{tenantId:guid}/capture-retention")]
public sealed class CaptureRetentionController(
    IMediator mediator,
    ICaptureRetentionQueries queries,
    ILogger<CaptureRetentionController> logger) : BaseController(logger)
{
    /// <summary>
    /// A política em vigor. Nunca 404: quem nunca configurou recebe o padrão — desligada, com a
    /// janela padrão —, e a tela não precisa inventar um estado inicial.
    /// </summary>
    [HttpGet]
    [ProtectedResource("capture-retention", "view")]
    public async Task<ActionResult<CaptureRetentionPolicyDto>> Get(
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken)
        => OkResponse(await queries.GetAsync(tenantId, cancellationToken));

    /// <summary>Liga ou desliga a purga e escolhe o prazo.</summary>
    [HttpPut]
    [ProtectedResource("capture-retention", "manage")]
    public async Task<ActionResult<ConfigureCaptureRetentionResponse>> Configure(
        [FromRoute] Guid tenantId,
        [FromBody] ConfigureCaptureRetentionModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId);
        var identified = new IdentifiedCommand<ConfigureCaptureRetentionCommand, ConfigureCaptureRetentionResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }
}
