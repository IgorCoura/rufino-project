namespace BillPayment.API.Controllers;

using BillPayment.API.Authorization;
using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Models.CaptureItems;
using BillPayment.Application.Queries.CaptureItems;
using BillPayment.Application.Queries.CapturedMessages;
using BillPayment.Domain.Extraction;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Itens capturados do tenant, incluindo a quarentena.
/// </summary>
/// <remarks>
/// <para>
/// O conteúdo devolvido é filtrado pelo status do item, e quem decide isso é o domínio
/// (<c>CaptureItemStatus.ExposesFinancialDetail</c>), não este controller nem a tela.
/// </para>
/// <para>
/// <c>reprocess</c> tem escopo PRÓPRIO, e não é zelo: reprocessar chama o extrator de visão, que
/// consome cota de uma conta com teto diário. Sob o mesmo escopo da leitura, quem só revisa a
/// quarentena poderia queimar o teto do dia e parar a captura de todo mundo.
/// </para>
/// </remarks>
[Route("api/v1/{tenantId:guid}/capture-items")]
public sealed class CaptureItemsController(
    IMediator mediator,
    ICaptureItemQueries queries,
    ICapturedMessageQueries capturedMessages,
    ILogger<CaptureItemsController> logger) : BaseController(logger)
{
    /// <summary>
    /// Lista os itens capturados. <paramref name="status"/> monta a fila de quarentena —
    /// <c>Unrouted</c> é a fila de reivindicação do dono da fonte.
    /// </summary>
    [HttpGet]
    [ProtectedResource("capture-item", "view")]
    public async Task<ActionResult<CaptureItemPage>> List(
        [FromRoute] Guid tenantId,
        [FromQuery] string? status,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListAsync(tenantId, status, cursor, limit, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProtectedResource("capture-item", "view")]
    public async Task<ActionResult<CaptureItemDto>> GetById(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var item = await queries.GetAsync(tenantId, id, cancellationToken);
        return item is null ? NotFound() : OkResponse(item);
    }

    /// <summary>
    /// Serve o documento original do item, para uma pessoa conferir antes de reivindicar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Reivindicar sem ver o documento é decidir no escuro</strong>, e é o oposto do que
    /// a tela de aprovação faz do outro lado do fluxo. Quem responde se o item pode ser aberto é
    /// a query, pelo mesmo gate do DTO (<c>ExposesFinancialDetail</c>).
    /// </para>
    /// <para>
    /// <c>404</c> cobre todas as negativas — item de outro tenant, item de outro pagador, item
    /// sem arquivo guardado e chave órfã. Distinguir confirmaria a existência do item a quem não
    /// pode vê-lo.
    /// </para>
    /// </remarks>
    [HttpGet("{id:guid}/artifact")]
    [ProtectedResource("capture-item", "view")]
    public async Task<IActionResult> GetArtifact(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var artifact = await queries.GetArtifactAsync(tenantId, id, cancellationToken);
        if (artifact is null)
            return NotFound();

        ArtifactAccessLog(tenantId, "capture-item", id);

        // O Stream é entregue ao pipeline, que o fecha ao terminar de escrever a resposta — por
        // isso não há `using` aqui: liberá-lo agora mandaria um corpo vazio.
        return File(artifact.Content, artifact.ContentType, artifact.FileName, enableRangeProcessing: true);
    }

    /// <summary>
    /// O e-mail que trouxe este item — título, remetente e corpo renderizável. 404 para anexo
    /// manual (não há e-mail por trás) e para mensagem já fora de alcance; mesmo portão do
    /// documento original (ADR-008), com o acesso registrado.
    /// </summary>
    [HttpGet("{id:guid}/email")]
    [ProtectedResource("capture-item", "view")]
    public async Task<ActionResult<CapturedMessageBodyDto>> GetEmail(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var body = await capturedMessages.GetBodyForCaptureItemAsync(tenantId, id, cancellationToken);
        if (body is null)
            return NotFound();

        ArtifactAccessLog(tenantId, "capture-item-email", id);

        return OkResponse(body);
    }

    /// <summary>
    /// Devolve o artefato à fila para a cascata de hoje avaliá-lo de novo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// O desfecho de um item é do dia em que ele passou: a cascata ganha degraus, o prompt muda e
    /// o cadastro muda — sem <c>PayerProfile</c> não há senha derivada, e sem <c>Payee</c> nem
    /// <c>TrustedOrigin</c> o que o parser erra é descartado. Sem este endpoint, reavaliar exigia
    /// apagar linha no banco.
    /// </para>
    /// <para>
    /// <strong>Um por vez, de propósito</strong>: a extração por visão custa por documento e a
    /// conta tem teto diário — reabrir a quarentena inteira queimaria a cota antes de chegar nos
    /// itens que interessam.
    /// </para>
    /// </remarks>
    [HttpPost("{id:guid}/reprocess")]
    [ProtectedResource("capture-item", "reprocess")]
    public async Task<ActionResult<ReprocessCaptureItemResponse>> Reprocess(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new ReprocessCaptureItemCommand(tenantId, id);
        var identified = new IdentifiedCommand<ReprocessCaptureItemCommand, ReprocessCaptureItemResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Assume que um item <c>Unrouted</c> é desta conta, e o promove a boleto.
    /// </summary>
    /// <remarks>
    /// <para>
    /// É o degrau 4 da escada: a escada não descobriu de quem é, e uma pessoa decide. A
    /// <c>Bill</c> nasce com <c>TenantRouting = Claimed</c>, que aparece como
    /// <c>Inconclusive</c> na tela de aprovação — reivindicar não pula a aprovação, só resolve a
    /// atribuição.
    /// </para>
    /// <para>
    /// Recusas, ambas <c>409</c>: pagador extraído que contradiz esta conta (<c>BLP.CPI04</c> — a
    /// escada já sabia que não era dela) e boleto já sob gestão de outra conta
    /// (<c>BLP.BIL02</c>, com aviso genérico que não identifica quem).
    /// </para>
    /// </remarks>
    [HttpPost("{id:guid}/claim")]
    [ProtectedResource("capture-item", "claim")]
    public async Task<ActionResult<ClaimCaptureItemResponse>> Claim(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new ClaimCaptureItemCommand(tenantId, id, ResolveDecidingUserId());
        var identified = new IdentifiedCommand<ClaimCaptureItemCommand, ClaimCaptureItemResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Reprova o item: uma pessoa olhou e não reconhece a cobrança.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É o que torna a fila de quarentena esvaziável.</strong> Sem isto, todo item que a
    /// cascata não resolve fica pendente para sempre e a lista deixa de ser olhada.
    /// </para>
    /// <para>
    /// Reversível por <c>reopen</c>, e com autor registrado: reprovar tira trabalho da vista sem
    /// que ninguém tenha conferido o documento, e é a única operação da quarentena com essa
    /// propriedade. Usa o escopo de <c>claim</c> porque reivindicar e reprovar são as duas faces
    /// da mesma decisão — resolver este item.
    /// </para>
    /// </remarks>
    [HttpPost("{id:guid}/dismiss")]
    [ProtectedResource("capture-item", "claim")]
    public async Task<ActionResult<DismissCaptureItemResponse>> Dismiss(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] DismissCaptureItemModel? model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new DismissCaptureItemCommand(tenantId, id, ResolveDecidingUserId(), model?.Note);
        var identified = new IdentifiedCommand<DismissCaptureItemCommand, DismissCaptureItemResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Anexa à mão o boleto que o sistema não conseguiu buscar, e devolve o item à fila.
    /// </summary>
    /// <remarks>
    /// Fecha o caminho que a escada de link não alcança — emissor com página atrás de login, ou
    /// sem receita cadastrada. A pessoa abre a URL que a quarentena agora mostra, baixa o PDF e o
    /// devolve aqui; daí em diante o fluxo é o de sempre: cascata, roteamento e aprovação.
    /// </remarks>
    [HttpPost("{id:guid}/artifact")]
    [ProtectedResource("capture-item", "reprocess")]
    [RequestSizeLimit(DocumentPayload.MAX_BYTES)]
    public async Task<ActionResult<AttachCaptureItemArtifactResponse>> AttachArtifact(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        IFormFile file,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { errors = new[] { new { code = "BLP.CPI09", message = "Envie o arquivo do boleto." } } });

        // Lido para memória porque o teto já é de 20 MB e o comando precisa dos bytes duas vezes
        // — para o hash e para a gravação. Acima disso o Kestrel recusa antes de chegar aqui.
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);

        var command = new AttachCaptureItemArtifactCommand(
            tenantId, id, buffer.ToArray(), file.ContentType, file.FileName);

        var identified = new IdentifiedCommand<AttachCaptureItemArtifactCommand, AttachCaptureItemArtifactResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }
}