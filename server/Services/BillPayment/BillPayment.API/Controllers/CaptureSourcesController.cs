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
        var command = model.ToCommand(tenantId);
        var identified = new IdentifiedCommand<ConnectCaptureSourceCommand, ConnectCaptureSourceResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPut("{id:guid}/name")]
    public async Task<ActionResult<RenameCaptureSourceResponse>> Rename(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] RenameCaptureSourceModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId, id);
        var identified = new IdentifiedCommand<RenameCaptureSourceCommand, RenameCaptureSourceResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    [HttpPut("{id:guid}/activation")]
    public async Task<ActionResult<AlterCaptureSourceActivationResponse>> AlterActivation(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] AlterCaptureSourceActivationModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId, id);
        var identified = new IdentifiedCommand<AlterCaptureSourceActivationCommand, AlterCaptureSourceActivationResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
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
        var command = model.ToCommand(tenantId, id);
        var identified = new IdentifiedCommand<ReplaceCaptureSourceCredentialCommand, ReplaceCaptureSourceCredentialResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
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
        var command = model.ToCommand(tenantId, id);
        var identified = new IdentifiedCommand<ChangeCaptureSourceFolderCommand, ChangeCaptureSourceFolderResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
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
        var command = new SyncCaptureSourceCommand(tenantId, id);
        var identified = new IdentifiedCommand<SyncCaptureSourceCommand, SyncCaptureSourceResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Acrescenta uma pasta à lista acompanhada. Corpo vazio ou <c>folderPath</c> nulo = caixa de entrada.
    /// </summary>
    /// <remarks>
    /// A pasta nasce sem cursor, então a primeira varredura dela lê tudo o que já está lá. Não há
    /// recursão: subpasta que não estiver na lista não é lida.
    /// </remarks>
    [HttpPost("{id:guid}/folders")]
    public async Task<ActionResult<AddCaptureSourceFolderResponse>> AddFolder(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] AddCaptureSourceFolderModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(tenantId, id);
        var identified = new IdentifiedCommand<AddCaptureSourceFolderCommand, AddCaptureSourceFolderResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Deixa de acompanhar uma pasta. Recusa remover a última (<c>BLP.CPS18</c>).
    /// </summary>
    /// <remarks>
    /// O caminho vai em query string, não no path: nome de pasta contém <c>/</c> por definição e
    /// no segmento de rota morreria em 404 antes do controller. Ausente = a caixa de entrada.
    /// </remarks>
    [HttpDelete("{id:guid}/folders")]
    public async Task<ActionResult<RemoveCaptureSourceFolderResponse>> RemoveFolder(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromQuery] string? folderPath,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveCaptureSourceFolderCommand(tenantId, id, folderPath);
        var identified = new IdentifiedCommand<RemoveCaptureSourceFolderCommand, RemoveCaptureSourceFolderResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Descarta o cursor de todas as pastas: a próxima varredura relê a caixa inteira.
    /// </summary>
    /// <remarks>
    /// Serve para reavaliar o que já passou depois de mudar o cadastro — sem <c>PayerProfile</c>
    /// não há senha derivada, e sem <c>Payee</c>/<c>TrustedOrigin</c> o que a cascata não
    /// reconhece é descartado em vez de ir para a quarentena. Reler <strong>não duplica</strong>:
    /// a ingestão é idempotente por <c>(tenant, fonte, mensagem, anexo)</c>.
    /// </remarks>
    [HttpPost("{id:guid}/rescan")]
    public async Task<ActionResult<RescanCaptureSourceResponse>> Rescan(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new RescanCaptureSourceCommand(tenantId, id);
        var identified = new IdentifiedCommand<RescanCaptureSourceCommand, RescanCaptureSourceResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>Desconecta a fonte e apaga a credencial. Os itens já ingeridos permanecem.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<DisconnectCaptureSourceResponse>> Disconnect(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new DisconnectCaptureSourceCommand(tenantId, id);
        var identified = new IdentifiedCommand<DisconnectCaptureSourceCommand, DisconnectCaptureSourceResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }
}
