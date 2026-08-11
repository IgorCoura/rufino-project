namespace BillPayment.API.Controllers;

using BillPayment.Application.CaptureSources.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Models.CaptureSources;
using BillPayment.Application.Queries.CaptureSources;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Fontes de captura do tenant — as caixas de e-mail monitoradas.
/// </summary>
/// <remarks>
/// <para>
/// Nenhuma resposta daqui devolve credencial nem o ponteiro do cofre (ADR-009); o contrato de
/// leitura informa apenas que a fonte <em>tem</em> credencial.
/// </para>
/// <para>
/// Autorização granular (<c>[ProtectedResource("capture-source", "view"|"manage")]</c>) entra na
/// fase 6, junto com o Keycloak. Até lá os endpoints estão abertos — ver "Checklist
/// pré-produção" no CLAUDE.md do BC.
/// </para>
/// </remarks>
[Route("api/v1/{tenantId:guid}/capture-sources")]
public sealed class CaptureSourcesController(
    IMediator mediator,
    ICaptureSourceQueries queries,
    ILogger<CaptureSourcesController> logger) : BaseController(logger)
{
    [HttpGet]
    public async Task<ActionResult<CaptureSourcePage>> List(
        [FromRoute] Guid tenantId,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListAsync(tenantId, cursor, limit, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CaptureSourceDto>> GetById(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var source = await queries.GetAsync(tenantId, id, cancellationToken);
        return source is null ? NotFound() : OkResponse(source);
    }

    /// <summary>
    /// Conecta a caixa. O acesso é provado <strong>antes</strong> de a fonte existir, e a
    /// resposta traz o aviso genérico de caixa já monitorada por outra conta (ADR-008).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ConnectCaptureSourceResponse>> Connect(
        [FromRoute] Guid tenantId,
        [FromBody] ConnectCaptureSourceModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<ConnectCaptureSourceCommand, ConnectCaptureSourceResponse>(
            model.ToCommand(tenantId), EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }

    [HttpPut("{id:guid}/name")]
    public async Task<ActionResult<RenameCaptureSourceResponse>> Rename(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] RenameCaptureSourceModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<RenameCaptureSourceCommand, RenameCaptureSourceResponse>(
            model.ToCommand(tenantId, id), EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }

    [HttpPut("{id:guid}/activation")]
    public async Task<ActionResult<AlterCaptureSourceActivationResponse>> AlterActivation(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] AlterCaptureSourceActivationModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<AlterCaptureSourceActivationCommand, AlterCaptureSourceActivationResponse>(
            model.ToCommand(tenantId, id), EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }

    /// <summary>Rotação do segredo, ou reconexão depois de uma revogação.</summary>
    [HttpPut("{id:guid}/credential")]
    public async Task<ActionResult<ReplaceCaptureSourceCredentialResponse>> ReplaceCredential(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] ReplaceCaptureSourceCredentialModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<ReplaceCaptureSourceCredentialCommand, ReplaceCaptureSourceCredentialResponse>(
            model.ToCommand(tenantId, id), EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }

    /// <summary>
    /// Aponta a fonte para outra pasta da caixa. Descarta o cursor, porque a varredura
    /// incremental do provedor e por pasta.
    /// </summary>
    [HttpPut("{id:guid}/folder")]
    public async Task<ActionResult<ChangeCaptureSourceFolderResponse>> ChangeFolder(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] ChangeCaptureSourceFolderModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<ChangeCaptureSourceFolderCommand, ChangeCaptureSourceFolderResponse>(
            model.ToCommand(tenantId, id), EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }

    /// <summary>
    /// Dispara a varredura desta fonte agora, sem esperar o agendador.
    /// </summary>
    /// <remarks>
    /// Existe para o usuário conferir a conexão logo depois de conectar, e é por ele que a suíte
    /// de integração dirige a sincronização de forma determinística — o agendador fica desligado
    /// nos testes, como o worker do outbox.
    /// </remarks>
    [HttpPost("{id:guid}/sync")]
    public async Task<ActionResult<SyncCaptureSourceResponse>> Sync(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<SyncCaptureSourceCommand, SyncCaptureSourceResponse>(
            new SyncCaptureSourceCommand(tenantId, id), EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }

    /// <summary>Desconecta a fonte e apaga a credencial. Os itens já ingeridos permanecem.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<DisconnectCaptureSourceResponse>> Disconnect(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<DisconnectCaptureSourceCommand, DisconnectCaptureSourceResponse>(
            new DisconnectCaptureSourceCommand(tenantId, id), EnsureRequestId(requestId));

        return OkResponse(await mediator.Send(identified, cancellationToken));
    }
}
