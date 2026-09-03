namespace BillPayment.API.Controllers;

using BillPayment.API.Authorization;
using BillPayment.API.Extension;
using BillPayment.Application.Bills.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Models.Bills;
using BillPayment.Application.Queries.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Extraction;
using BillPayment.Application.Queries.CapturedMessages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Entrada e leitura de documentos de cobrança.
/// </summary>
/// <remarks>
/// <c>bill:approve</c> é a ação sensível deste BC — é ela que autoriza dinheiro sair (ADR-007),
/// e por isso tem escopo próprio em vez de cair num <c>manage</c> junto com o resto. O mesmo
/// vale para <c>deny</c> e <c>cancel</c>: quem só importa boleto não decide o destino dele.
/// </remarks>
[Route("api/v1/{tenantId:guid}/bills")]
public sealed class BillsController(
    IMediator mediator,
    IBillQueries queries,
    ICapturedMessageQueries capturedMessages,
    IPaymentSchedulePreviewQueries schedulePreviews,
    IAuthorizationServerClient authorizationServer,
    ILogger<BillsController> logger) : BaseController(logger)
{
    // Alçada de aprovação por nível de risco, do maior para o menor. Quem só tem bill:approve
    // (a porta de entrada do endpoint) aprova Verde; cada escopo destes cobre os níveis abaixo.
    private static readonly string[] RiskClearanceScopes = ["approve-extreme", "approve-danger", "approve-attention"];

    [HttpGet]
    [ProtectedResource("bill", "view")]
    public async Task<ActionResult<BillPage>> List(
        [FromRoute] Guid tenantId,
        [FromQuery] string? status,
        [FromQuery] string? cursor,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
        => OkResponse(await queries.ListAsync(tenantId, status, cursor, limit, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProtectedResource("bill", "view")]
    public async Task<ActionResult<BillDto>> GetById(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var bill = await queries.GetAsync(tenantId, id, cancellationToken);
        return bill is null ? NotFound() : OkResponse(bill);
    }

    /// <summary>Importa um documento a partir da linha digitável, do QR Pix, ou dos dois.</summary>
    /// <remarks>
    /// Esta é a forma JSON, para quem já tem os dígitos. A mesma rota aceita
    /// <c>multipart/form-data</c> quando o arquivo do boleto acompanha — ver
    /// <see cref="ImportWithDocument"/>.
    /// </remarks>
    [HttpPost("import")]
    [EnableRateLimiting(RateLimitingExtensions.EXPENSIVE_POLICY)]
    [Consumes("application/json")]
    [ProtectedResource("bill", "import")]
    public async Task<ActionResult<ImportBillResponse>> Import(
        [FromRoute] Guid tenantId,
        [FromBody] ImportBillModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
        => await DispatchImportAsync(tenantId, model.ToCommand(tenantId), requestId, cancellationToken);

    /// <summary>
    /// Importa um documento a partir do arquivo do boleto, acompanhado ou não dos dígitos.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Divide a rota com <see cref="Import"/>: quem escolhe a action é o <c>Content-Type</c> da
    /// requisição, via <c>[Consumes]</c>. Uma action só não resolve — <c>IFormFile</c> e
    /// <c>[FromBody]</c> não convivem no mesmo binder —, e uma rota nova obrigaria o cliente a
    /// saber de antemão qual chamar para o que é a mesma operação.
    /// </para>
    /// <para>
    /// O arquivo é opcional aqui, para o formulário da tela poder mandar sempre pelo mesmo
    /// caminho: sem ele, a importação é a mesma da forma JSON.
    /// </para>
    /// </remarks>
    [HttpPost("import")]
    [EnableRateLimiting(RateLimitingExtensions.EXPENSIVE_POLICY)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(DocumentPayload.MAX_BYTES)]
    [ProtectedResource("bill", "import")]
    public async Task<ActionResult<ImportBillResponse>> ImportWithDocument(
        [FromRoute] Guid tenantId,
        [FromForm] ImportBillFormModel model,
        IFormFile? file,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        // Lido para memória porque o teto já é de 20 MB e o handler precisa dos bytes duas vezes
        // — para o hash e para a gravação. Acima disso o Kestrel recusa antes de chegar aqui.
        ReadOnlyMemory<byte> content = default;
        if (file is not null && file.Length > 0)
        {
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, cancellationToken);
            content = buffer.ToArray();
        }

        var command = model.ToCommand(tenantId, content, file?.ContentType, file?.FileName);

        return await DispatchImportAsync(tenantId, command, requestId, cancellationToken);
    }

    private async Task<ActionResult<ImportBillResponse>> DispatchImportAsync(
        Guid tenantId,
        ImportBillCommand command,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<ImportBillCommand, ImportBillResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(tenantId, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, tenantId, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Serve o documento original que deu origem ao boleto.
    /// </summary>
    /// <remarks>
    /// <para>
    /// É o papel contra o qual o aprovador confere as doze verificações. <strong>Não é a linha
    /// digitável</strong>: o arquivo é o comprovante do que o sistema viu, e continua valendo que
    /// os dígitos nunca saem por esta API — quem os tem, paga.
    /// </para>
    /// <para>
    /// Documento que o emissor trancou sai <strong>sem senha</strong>: a que abre foi derivada do
    /// cadastro do tenant, e pedi-la a quem aprova seria pedir o que o sistema já sabe.
    /// </para>
    /// <para>
    /// <c>404</c> quando o boleto não é deste tenant e quando não há arquivo — importação manual
    /// nasce só com os dígitos, e isso é estado normal, não falha.
    /// </para>
    /// </remarks>
    [HttpGet("{id:guid}/artifact")]
    [ProtectedResource("bill", "view")]
    public async Task<IActionResult> GetArtifact(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var artifact = await queries.GetArtifactAsync(tenantId, id, cancellationToken);
        if (artifact is null)
            return NotFound();

        ArtifactAccessLog(tenantId, "bill", id, artifact.Unlocked);

        return File(artifact.Content, artifact.ContentType, artifact.FileName, enableRangeProcessing: true);
    }

    /// <summary>
    /// O e-mail que trouxe este boleto — título, remetente e corpo renderizável. 404 para boleto
    /// que não veio de caixa de e-mail; mesmo portão do documento original (ADR-008).
    /// </summary>
    [HttpGet("{id:guid}/email")]
    [ProtectedResource("bill", "view")]
    public async Task<ActionResult<CapturedMessageBodyDto>> GetEmail(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var body = await capturedMessages.GetBodyForBillAsync(tenantId, id, cancellationToken);
        if (body is null)
            return NotFound();

        ArtifactAccessLog(tenantId, "bill-email", id);

        return OkResponse(body);
    }

    /// <summary>Detalhe com as doze verificações e a evidência de cada uma.</summary>
    [HttpGet("{id:guid}/detail")]
    [ProtectedResource("bill", "view")]
    public async Task<ActionResult<BillDetailDto>> GetDetail(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var detail = await queries.GetDetailAsync(tenantId, id, cancellationToken);
        return detail is null ? NotFound() : OkResponse(detail);
    }

    /// <summary>
    /// A prévia do agendamento para o sheet de aprovar: a data em que a submissão realmente
    /// ocorreria (mesma política/calendário/fuso da fila), com o deslize e o caso imediato
    /// explícitos ANTES de o aprovador autorizar. Leitura pura — nada muda.
    /// </summary>
    [HttpGet("{id:guid}/schedule-preview")]
    [ProtectedResource("bill", "view")]
    public async Task<ActionResult<SchedulePreviewDto>> GetSchedulePreview(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        if (date is null)
            return BadRequest();

        var preview = await schedulePreviews.PreviewAsync(tenantId, id, date.Value, cancellationToken);
        return preview is null ? NotFound() : OkResponse(preview);
    }

    /// <summary>
    /// Reexecuta a consulta oficial e as verificações. É o botão de revalidar da tela, e o
    /// caminho para o retrato voltar a estar dentro do prazo antes de aprovar.
    /// </summary>
    [HttpPost("{id:guid}/revalidate")]
    [EnableRateLimiting(RateLimitingExtensions.EXPENSIVE_POLICY)]
    [ProtectedResource("bill", "validate")]
    public async Task<ActionResult<ValidateBillResponse>> Revalidate(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new ValidateBillCommand(tenantId, id);
        var identified = new IdentifiedCommand<ValidateBillCommand, ValidateBillResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Relê o documento original (e o corpo do e-mail) pelo extrator de IA e anexa o retrato da
    /// leitura — o backfill dos boletos nascidos antes da leitura, um por chamada.
    /// </summary>
    [HttpPost("{id:guid}/enrich")]
    [EnableRateLimiting(RateLimitingExtensions.EXPENSIVE_POLICY)]
    [ProtectedResource("bill", "validate")]
    public async Task<ActionResult<EnrichBillReadingResponse>> Enrich(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new EnrichBillReadingCommand(tenantId, id);
        var identified = new IdentifiedCommand<EnrichBillReadingCommand, EnrichBillReadingResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>Autoriza o pagamento. Nenhum boleto é pago sem passar por aqui (ADR-007).</summary>
    [HttpPost("{id:guid}/approve")]
    [ProtectedResource("bill", "approve")]
    public async Task<ActionResult<ApproveBillResponse>> Approve(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] ApproveBillModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCommand(
            tenantId, id, ResolveDecidingUserId(), await ResolveRiskClearanceAsync(cancellationToken));
        var identified = new IdentifiedCommand<ApproveBillCommand, ApproveBillResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>Recusa o boleto. O motivo é obrigatório.</summary>
    [HttpPost("{id:guid}/deny")]
    [ProtectedResource("bill", "deny")]
    public async Task<ActionResult<DenyBillResponse>> Deny(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] BillDecisionModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToDenyCommand(tenantId, id, ResolveDecidingUserId());
        var identified = new IdentifiedCommand<DenyBillCommand, DenyBillResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>Tira o boleto do fluxo — inclusive um que nem chegou a ser verificado.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProtectedResource("bill", "cancel")]
    public async Task<ActionResult<CancelBillResponse>> Cancel(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] BillDecisionModel model,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = model.ToCancelCommand(tenantId, id, ResolveDecidingUserId());
        var identified = new IdentifiedCommand<CancelBillCommand, CancelBillResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Devolve um boleto de pagamento FALHADO à fila de decisão (fase 3). A nova tentativa é
    /// uma nova aprovação e uma nova ordem (ADR-002) — por isso o escopo é o de quem aprova.
    /// </summary>
    [HttpPost("{id:guid}/reopen")]
    [ProtectedResource("bill", "approve")]
    public async Task<ActionResult<ReopenBillResponse>> Reopen(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromHeader(Name = "x-requestid")] Guid requestId,
        CancellationToken cancellationToken)
    {
        var command = new ReopenBillCommand(tenantId, id);
        var identified = new IdentifiedCommand<ReopenBillCommand, ReopenBillResponse>(
            command, EnsureRequestId(requestId));

        SendingCommandLog(id, command, identified.Id);
        var result = await mediator.Send(identified, cancellationToken);
        CommandResultLog(result, id, command, identified.Id);

        return OkResponse(result);
    }

    /// <summary>
    /// Resolve a alçada de risco de quem aprova: uma pergunta UMA pelos três escopos, e o maior
    /// concedido vira a alçada (hierárquica — cobre os níveis abaixo). Sem nenhum, a alçada é
    /// Verde, que o <c>bill:approve</c> da porta de entrada já garante. Quem COMPARA alçada com
    /// o risco atual do boleto é o domínio (BLP.BIL32) — aqui só se descobre quem a pessoa é.
    /// </summary>
    private async Task<string> ResolveRiskClearanceAsync(CancellationToken cancellationToken)
    {
        var granted = await authorizationServer.GetGrantedScopesAsync(
            "bill", RiskClearanceScopes, cancellationToken);

        if (granted.Contains("approve-extreme"))
            return RiskLevel.ExtremeDanger.Name;
        if (granted.Contains("approve-danger"))
            return RiskLevel.Danger.Name;
        if (granted.Contains("approve-attention"))
            return RiskLevel.Attention.Name;

        return RiskLevel.Safe.Name;
    }
}
