namespace BillPayment.Application.CaptureItems.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.TrustedOrigins;
using Microsoft.Extensions.Logging;

/// <summary>
/// Baixa o artefato, roda a cascata de extração e decide o destino do item.
/// </summary>
/// <remarks>
/// <para>
/// É o passo que transforma um <c>CaptureItem</c> recém-ingerido em boleto candidato — ou o faz
/// desaparecer. Quem decide o destino é o <c>CaptureTriageService</c>; aqui só há orquestração.
/// </para>
/// <para>
/// <strong>O artefato só é guardado quando há instrumento válido.</strong> A cascata só conclui
/// que um anexo não interessa depois de já tê-lo baixado, e guardar tudo transformaria o balde
/// num depósito de documento pessoal — medido no corpus real: 8 de 11 anexos da primeira página
/// de uma caixa de uso misto não eram conta a pagar, incluindo CNH e contrato.
/// </para>
/// </remarks>
public sealed record ProcessCaptureItemCommand(Guid TenantId, Guid CaptureItemId)
    : IRequest<ProcessCaptureItemResponse>;

/// <param name="Decision">
/// <c>Parse</c>, <c>Lock</c>, <c>Quarantine</c> ou <c>Drop</c> — e <c>Drop</c> significa que o
/// item deixou de existir.
/// </param>
public sealed record ProcessCaptureItemResponse(Guid Id, string Decision, int InstrumentsFound);

public sealed class ProcessCaptureItemCommandHandler(
    ICaptureItemRepository items,
    ICaptureSourceRepository sources,
    ITrustedOriginRepository origins,
    IPayerProfileRepository payerProfiles,
    IMailboxReader mailboxReader,
    IBoletoDocumentParser parser,
    IAttachmentStorage storage,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<ProcessCaptureItemCommandHandler> logger)
    : IRequestHandler<ProcessCaptureItemCommand, ProcessCaptureItemResponse>
{
    public async Task<ProcessCaptureItemResponse> Handle(
        ProcessCaptureItemCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var itemId = CaptureItemId.From(request.CaptureItemId);

        var item = await items.GetAsync(tenantId, itemId, cancellationToken)
            ?? throw CaptureItemErrors.NotFound(request.CaptureItemId);

        var source = await sources.GetAsync(tenantId, item.SourceId, cancellationToken)
            ?? throw CaptureSourceErrors.NotFound(item.SourceId.Value);

        var now = clock.GetUtcNow();

        var content = await mailboxReader.DownloadArtifactAsync(
            source.Address, source.Credential!, item.ExternalMessageId, item.ArtifactKey, cancellationToken);

        // Vazio conta como ausente: um adapter que devolve zero byte nao entregou o artefato,
        // e seguir com ele faria a cascata concluir "nao e boleto" sobre um nada.
        if (content is null || content.Value.IsEmpty)
        {
            // Anexo que não veio não é "não é boleto": nada se aprendeu sobre ele, e descartar
            // perderia um documento que a próxima tentativa traria.
            item.MarkLinkFailed("artifact_download_failed", now.UtcDateTime);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);

            return new ProcessCaptureItemResponse(item.Id.Value, "DownloadFailed", 0);
        }

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);
        var passwords = PasswordDerivationService.Derive(profile);

        var extraction = await parser.ParseAsync(
            content.Value,
            contentType: null,
            passwords,
            DateOnly.FromDateTime(now.UtcDateTime),
            cancellationToken);

        var origin = await ResolveOriginAsync(item, tenantId, cancellationToken);
        var decision = CaptureTriageService.Decide(extraction, origin);

        await ApplyAsync(item, decision, extraction, content.Value, tenantId, now.UtcDateTime, cancellationToken);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ProcessCaptureItemResponse(item.Id.Value, decision.Name, extraction.Instruments.Count);
    }

    /// <summary>
    /// Executa o que a triagem decidiu — inclusive fazer o item deixar de existir.
    /// </summary>
    /// <remarks>
    /// <strong>Só o <c>Parse</c> guarda o arquivo.</strong> Nos demais o artefato é descartado
    /// junto com a decisão: o que não é boleto não precisa ser protegido, cifrado nem apagado
    /// depois. É a retenção por desfecho, decidida com o usuário em 2026-08-11.
    /// </remarks>
    private async Task ApplyAsync(
        CaptureItem item,
        CaptureTriageDecision decision,
        ExtractionResult extraction,
        ReadOnlyMemory<byte> content,
        TenantId tenantId,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        if (decision == CaptureTriageDecision.Drop)
        {
            // Some sem deixar rastro nem arquivo. É o desfecho mais comum numa caixa de uso
            // misto, e é o que mantém a fila de quarentena utilizável por uma pessoa.
            items.Remove(item);
            return;
        }

        if (decision == CaptureTriageDecision.Parse)
        {
            var storageKey = await storage.StoreAsync(
                tenantId, item.ArtifactKey, "application/pdf", content, cancellationToken);

            var hash = Sha256Of(content.Span);

            item.StoreArtifact(hash, storageKey, occurredAt);
            item.MarkParsed(extraction.Method!, extraction.UnlockedBy, occurredAt);
            return;
        }

        // Lock e Quarantine mantêm o item para uma pessoa resolver, mas sem o arquivo: o que
        // ela precisa ver é remetente, assunto e data — e a chave digitada à mão não depende
        // de o original estar guardado.
        if (decision == CaptureTriageDecision.Lock)
        {
            item.StoreArtifact(Sha256Of(content.Span), "pending-unlock", occurredAt);
            item.MarkLocked(occurredAt);
            return;
        }

        // Unrecognized, e nao Unrouted: o parser nao reconheceu boleto, que e coisa diferente
        // de nao saber de quem e o boleto. E e de Unrecognized que sai o caminho previsto pelo
        // doc 09 — a pessoa informa a linha digitavel a mao e o item volta para Parsed.
        item.StoreArtifact(Sha256Of(content.Span), "pending-review", occurredAt);
        item.MarkUnrecognized(extraction.ReasonCode ?? "no_instrument", occurredAt);

        logger.LogInformation(
            "Item de remetente cadastrado ficou em quarentena sem instrumento reconhecido.");
    }

    private Task<TrustedOrigin?> ResolveOriginAsync(
        CaptureItem item,
        TenantId tenantId,
        CancellationToken cancellationToken)
        => string.IsNullOrWhiteSpace(item.Sender)
            ? Task.FromResult<TrustedOrigin?>(null)
            : origins.ResolveBySenderAsync(tenantId, item.Sender, cancellationToken);

    private static string Sha256Of(ReadOnlySpan<byte> content)
        => "sha256:" + Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(content));
}

public sealed class ProcessCaptureItemIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ProcessCaptureItemIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ProcessCaptureItemCommand, ProcessCaptureItemResponse>(mediator, requestManager, logger)
{
    protected override ProcessCaptureItemResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, 0);
}
