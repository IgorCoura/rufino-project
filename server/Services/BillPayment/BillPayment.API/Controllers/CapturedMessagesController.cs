namespace BillPayment.API.Controllers;

using BillPayment.API.Authorization;
using BillPayment.Application.CapturedMessages.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Queries.CapturedMessages;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// O livro-caixa da captura: todo e-mail lido, e o que o sistema decidiu sobre cada anexo.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Recurso de autorização próprio</strong> (<c>captured-message</c>), e não um escopo
/// pendurado em <c>capture-item</c>: são coisas diferentes. A quarentena é fila de trabalho e
/// mostra só o que ficou pendente; isto é histórico, e inclui o e-mail que o sistema descartou —
/// cujo item não existe mais.
/// </para>
/// <para>
/// <strong>Só metadado sai por aqui.</strong> Nem chave de armazenamento, nem link, nem
/// identificador permanente da mensagem: o que o histórico responde é "o que houve com o e-mail
/// que eu mandei", e nada além disso justifica trafegar.
/// </para>
/// </remarks>
[Route("api/v1/{tenantId:guid}/captured-messages")]
public sealed class CapturedMessagesController(
    IMediator mediator,
    ICapturedMessageQueries queries,
    ILogger<CapturedMessagesController> logger) : BaseController(logger)
{
    /// <summary>
    /// Lista os e-mails lidos, do mais recente para o mais antigo.
    /// </summary>
    /// <param name="outcome">
    /// Nome do desfecho (<c>Promoted</c>, <c>Discarded</c>, <c>Unrouted</c>…). Casa quando
    /// qualquer anexo tem aquele desfecho; valor desconhecido devolve tudo, como os demais
    /// filtros do BC.
    /// </param>
    /// <param name="search">Trecho do remetente ou do assunto, sem caixa.</param>
    [HttpGet]
    [ProtectedResource("captured-message", "view")]
    public async Task<ActionResult<CapturedMessagePage>> List(
        [FromRoute] Guid tenantId,
        [FromQuery] string? outcome,
        [FromQuery] Guid? sourceId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListAsync(
            tenantId, outcome, sourceId, from, to, search, cursor, limit, cancellationToken));

    /// <summary>Quando a caixa foi lida pela última vez — o cabeçalho da tela.</summary>
    [HttpGet("sync-status")]
    [ProtectedResource("captured-message", "view")]
    public async Task<ActionResult<CaptureSyncStatusDto>> SyncStatus(
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken)
        => OkResponse(await queries.GetSyncStatusAsync(tenantId, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProtectedResource("captured-message", "view")]
    public async Task<ActionResult<CapturedMessageDto>> GetById(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var message = await queries.GetAsync(tenantId, id, cancellationToken);
        return message is null ? NotFound() : OkResponse(message);
    }

    /// <summary>
    /// O e-mail inteiro — cabeçalho e corpo renderizável. O corpo pode carregar instrumento de
    /// pagamento, então a resposta sai sob o mesmo portão do documento original (ADR-008), com o
    /// acesso registrado.
    /// </summary>
    [HttpGet("{id:guid}/body")]
    [ProtectedResource("captured-message", "view")]
    public async Task<ActionResult<CapturedMessageBodyDto>> GetBody(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var body = await queries.GetBodyAsync(tenantId, id, cancellationToken);
        if (body is null)
            return NotFound();

        ArtifactAccessLog(tenantId, "captured-message-body", id);

        return OkResponse(body);
    }

    /// <summary>
    /// Apaga o que a captura produziu para este e-mail e o reingere como se fosse novo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Não é o mesmo que reprocessar um item.</strong> Reprocessar devolve à cascata um
    /// item que ainda existe, com os mesmos ids — e por isso não recupera nada quando o problema
    /// é o endereço de armazenamento ter morrido. A recaptura reencontra a mensagem pelo
    /// identificador permanente do cabeçalho e recomeça do zero.
    /// </para>
    /// <para>
    /// Escopo próprio (<c>recapture</c>) porque reingerir consome cota do extrator de visão, pela
    /// mesma razão que separou <c>capture-item:reprocess</c>.
    /// </para>
    /// </remarks>
    [HttpPost("{id:guid}/recapture")]
    [ProtectedResource("captured-message", "recapture")]
    public async Task<ActionResult<RecaptureMessageResponse>> Recapture(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        // Quem pede a recaptura é quem cancela o boleto ainda não decidido — a trilha do
        // cancelamento precisa de um UserId, e ele vem só do token (ADR-007).
        var command = new RecaptureMessageCommand(tenantId, id, ResolveDecidingUserId());
        var identified = new IdentifiedCommand<RecaptureMessageCommand, RecaptureMessageResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }
}
