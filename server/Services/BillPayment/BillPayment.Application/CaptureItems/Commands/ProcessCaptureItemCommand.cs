namespace BillPayment.Application.CaptureItems.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Payees;
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
/// <para>
/// <strong>É <c>IMultiAggregateCommand</c> desde a 2.6</strong>, e a justificativa é atomicidade
/// entre o item e o boleto que ele gera: <c>CaptureItem.Promote</c> guarda o <c>BillId</c>, então
/// criar a <c>Bill</c> noutra transação produziria — numa falha no meio — ou um boleto que item
/// nenhum aponta (invisível na fila e sem trilha de origem) ou um item marcado <c>Promoted</c>
/// apontando para um boleto que não existe. Consistência eventual por Domain Event não resolve:
/// o id do boleto é o próprio dado que precisa atravessar, e ele só existe depois da criação.
/// Como há um único <c>SaveEntitiesAsync</c>, a transação implícita do EF cobre os dois.
/// </para>
/// </remarks>
public sealed record ProcessCaptureItemCommand(Guid TenantId, Guid CaptureItemId)
    : IRequest<ProcessCaptureItemResponse>, IMultiAggregateCommand;

/// <param name="Decision">
/// <c>Parse</c>, <c>Lock</c>, <c>Quarantine</c> ou <c>Drop</c> — e <c>Drop</c> significa que o
/// item deixou de existir.
/// </param>
/// <param name="Routing">
/// O desfecho da escada de roteamento — <c>Promote</c>, <c>Foreign</c> ou <c>Unrouted</c>. Nulo
/// quando a cascata não chegou a achar boleto, e portanto não houve o que rotear.
/// </param>
public sealed record ProcessCaptureItemResponse(
    Guid Id,
    string Decision,
    int InstrumentsFound,
    string? Routing = null,
    Guid? BillId = null);

public sealed class ProcessCaptureItemCommandHandler(
    ICaptureItemRepository items,
    ICaptureSourceRepository sources,
    ITrustedOriginRepository origins,
    IPayerProfileRepository payerProfiles,
    IPayeeRepository payees,
    IBillRepository bills,
    IMailboxReader mailboxReader,
    IBoletoDocumentParser parser,
    IDocumentLinkResolver linkResolver,
    IDocumentIntelligence documentIntelligence,
    IAttachmentStorage storage,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<ProcessCaptureItemCommandHandler> logger)
    : IRequestHandler<ProcessCaptureItemCommand, ProcessCaptureItemResponse>
{
    /// <summary>
    /// A escada atribuiu o boleto a este tenant, mas ele já está sob gestão de outra conta.
    /// Genérico de propósito — a exceção 2 do doc 07 informa que existe, nunca de quem é.
    /// </summary>
    private const string BILL_UNDER_ANOTHER_ACCOUNT = "bill_under_another_account";

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
            item.ContentType,
            passwords,
            DateOnly.FromDateTime(now.UtcDateTime),
            cancellationToken);

        var origin = await ResolveOriginAsync(item, tenantId, cancellationToken);

        // O que é processado, guardado e mandado para a visão deixa de ser necessariamente o que
        // foi baixado: quando o artefato é o corpo de um e-mail e o boleto está atrás de um link,
        // é o documento buscado que assume o lugar dele daqui para a frente.
        var payload = content.Value;
        var payloadType = item.ContentType;

        // Degrau 2: só quando o corpo não trazia o instrumento escrito nele. Buscar o PDF de uma
        // fatura cujo Pix já está no texto seria gastar rede — e abrir superfície de ataque — para
        // descobrir o que já estava ali.
        if (!extraction.Resolved && !extraction.IsLocked)
        {
            var resolved = await linkResolver.ResolveAsync(payload, payloadType, cancellationToken);

            if (resolved is not null)
            {
                // A procedência é registrada mesmo que o documento buscado não resolva: saber
                // ONDE o sistema foi procurar é o que permite corrigir a receita depois.
                item.RecordResolvedLink(resolved.SourceUrl, now.UtcDateTime);

                payload = resolved.Content;
                payloadType = resolved.MediaType;

                extraction = await parser.ParseAsync(
                    payload, payloadType, passwords, DateOnly.FromDateTime(now.UtcDateTime), cancellationToken);
            }
        }

        // Degrau 3: só o que o determinístico não resolveu, e só quando vale gastar. PDF cifrado
        // não entra — mandar um arquivo que não abre gastaria a chamada para o modelo ver a tela
        // de senha.
        if (!extraction.Resolved && !extraction.IsLocked)
        {
            extraction = await TryVisionAsync(
                item, tenantId, profile, origin, payload, payloadType, now.UtcDateTime, cancellationToken)
                ?? extraction;
        }

        var decision = CaptureTriageService.Decide(extraction, origin);

        await ApplyAsync(
            item, decision, extraction, payload, payloadType, tenantId, now.UtcDateTime, cancellationToken);

        // A escada só roda sobre o que a cascata reconheceu como boleto: sem instrumento não há
        // o que rotear, e os demais desfechos já param no próprio estado que a triagem escolheu.
        var routing = decision == CaptureTriageDecision.Parse
            ? await RouteAsync(item, extraction, profile, tenantId, now.UtcDateTime, cancellationToken)
            : null;

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ProcessCaptureItemResponse(
            item.Id.Value,
            decision.Name,
            extraction.Instruments.Count,
            routing?.Outcome.Name,
            item.BillId?.Value);
    }

    /// <summary>
    /// Decide de quem é o boleto e aplica o desfecho — a escada de roteamento do doc 07.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Nenhum boleto vira <c>Bill</c> sem rota determinada.</strong> Não existe atribuição
    /// por default ao dono da fonte: uma caixa compartilhada traz a conta dos dois, e assumir que
    /// é de quem conectou é exatamente como um usuário acabaria pagando a conta do outro.
    /// </para>
    /// <para>
    /// Quem decide é o <c>BillRoutingService</c>. Aqui só se resolve o que ele não pode buscar —
    /// a exclusividade do beneficiário, que exige a travessia de tenant do ADR-008.
    /// </para>
    /// </remarks>
    private async Task<RoutingDecision> RouteAsync(
        CaptureItem item,
        ExtractionResult extraction,
        PayerProfile? profile,
        TenantId tenantId,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var exclusive = await ResolveExclusivePayeesAsync(tenantId, extraction, cancellationToken);
        var routing = BillRoutingService.Route(extraction, profile, exclusive);

        if (routing.Outcome == RoutingOutcome.Foreign)
        {
            item.MarkForeign(routing.Reason, occurredAt);
            return routing;
        }

        if (routing.Outcome == RoutingOutcome.Unrouted)
        {
            item.MarkUnrouted(routing.Reason, occurredAt);
            return routing;
        }

        await PromoteAsync(item, extraction, routing, tenantId, occurredAt, cancellationToken);
        return routing;
    }

    /// <summary>
    /// Beneficiários cadastrados por este tenant, e por mais ninguém, que aparecem no documento.
    /// </summary>
    /// <remarks>
    /// A pergunta cruza tenant e por isso passa pela travessia autorizada, que devolve
    /// <c>bool</c> — nunca quem é o outro. Um beneficiário que dois tenants cadastraram
    /// simplesmente não entra: a evidência vira ambígua e escolher seria adivinhar de quem é a
    /// conta.
    /// </remarks>
    private async Task<IReadOnlyCollection<TaxId>> ResolveExclusivePayeesAsync(
        TenantId tenantId,
        ExtractionResult extraction,
        CancellationToken cancellationToken)
    {
        if (extraction.Parties.Count == 0)
            return [];

        var registered = await payees.ListByTenantAsync(tenantId, cancellationToken);

        var inDocument = registered
            .Where(p => extraction.Parties.Any(c => c.TaxId.Equals(p.TaxId)))
            .Select(p => p.TaxId);

        var exclusive = new List<TaxId>();

        foreach (var taxId in inDocument)
        {
            if (!await payees.IsRegisteredByAnotherTenantAsync(tenantId, taxId, cancellationToken))
                exclusive.Add(taxId);
        }

        return exclusive;
    }

    /// <summary>
    /// O item vira boleto deste tenant, com o degrau que o atribuiu registrado.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Um compromisso é pago uma vez, globalmente.</strong> A sondagem de duplicata separa
    /// os dois casos que parecem iguais no índice e são opostos na intenção: o boleto já é
    /// <em>deste</em> tenant — reprocessamento do mesmo artefato, e o item volta a apontar para o
    /// boleto que já existe — ou é de outra conta, e aí este item não pode virar boleto nenhum.
    /// Sem essa distinção, reprocessar um item promovido o mandaria para a quarentena.
    /// </para>
    /// <para>
    /// O aviso do segundo caso é <strong>genérico por desenho</strong> (exceção 2 do doc 07): o
    /// usuário precisa saber que o documento já está sob gestão, e não de quem.
    /// </para>
    /// </remarks>
    private async Task PromoteAsync(
        CaptureItem item,
        ExtractionResult extraction,
        RoutingDecision routing,
        TenantId tenantId,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var bill = Bill.Capture(
            tenantId,
            extraction.Instruments,
            BillOrigin.Create(
                BillSourceKind.Mailbox,
                item.ReceivedAt,
                item.SourceId.Value,
                item.Sender,
                item.ExternalMessageId,
                item.ContentHash,
                item.StorageKey),
            occurredAt,
            routing.PayerTaxId is null ? null : PartyInfo.Of(name: null, routing.PayerTaxId),
            routing.Confidence);

        if (bill.DedupKey is not null)
        {
            var duplicate = await bills.ProbeActiveDuplicateAsync(
                bill.DedupKey, tenantId, bill.Id, cancellationToken);

            if (duplicate.OriginalBillId is { } original)
            {
                item.Promote(original, routing.Confidence!, occurredAt);
                return;
            }

            if (duplicate.Exists)
            {
                item.MarkUnrouted(BILL_UNDER_ANOTHER_ACCOUNT, occurredAt);
                return;
            }
        }

        await bills.AddAsync(bill, cancellationToken);
        item.Promote(bill.Id, routing.Confidence!, occurredAt);
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
        string? contentType,
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
            // O tipo guardado é o do que foi realmente lido: quando o boleto veio por link, o
            // artefato é o PDF buscado, e não o corpo do e-mail que apontava para ele.
            var storageKey = await storage.StoreAsync(
                tenantId, item.ArtifactKey, contentType ?? "application/pdf", content, cancellationToken);

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

    /// <summary>
    /// Degrau 3 da cascata: o extrator de visão propõe, o domínio dispõe.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Devolve <c>null</c> quando não vale a pena ou nada resolveu</strong>, para quem
    /// chamou preservar o motivo original da cascata determinística — <c>no_text_layer</c> diz
    /// mais para quem opera do que um motivo genérico de visão.
    /// </para>
    /// <para>
    /// <strong>O que volta do modelo não é boleto: é string.</strong> Quem converte é o
    /// <c>CandidateValidationService</c>, e ele só aceita o que sobrevive ao DV da linha
    /// digitável ou ao CRC do BR Code (ADR-011). Lista vazia depois de o modelo ter respondido é
    /// desfecho normal — inclusive quando ele alucinou, e é essa a defesa.
    /// </para>
    /// </remarks>
    private async Task<ExtractionResult?> TryVisionAsync(
        CaptureItem item,
        TenantId tenantId,
        PayerProfile? profile,
        TrustedOrigin? origin,
        ReadOnlyMemory<byte> content,
        string? contentType,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        if (!documentIntelligence.IsEnabled)
            return null;

        // O tipo é o DECLARADO na ingestão, nunca deduzido do nome: a chave do artefato é opaca
        // no provedor, e adivinhar dali rotulava toda imagem como PDF — o extrator recusava, e os
        // anexos que não eram PDF seguiam inalcançáveis mesmo com a visão existindo.
        if (!DocumentPayload.IsSupported(contentType))
            return null;

        // FileName, não ArtifactKey: a chave é opaca no provedor, então procurar sinal de cobrança
        // nela nunca casaria com nada — o portão ficaria decidindo só pelo assunto.
        if (!VisionGateService.ShouldAttempt(origin, item.Subject, item.FileName))
            return null;

        var hints = await BuildHintsAsync(tenantId, profile, item.Sender, cancellationToken);
        var extracted = await documentIntelligence.ExtractAsync(
            DocumentPayload.From(tenantId, content, contentType), hints, cancellationToken);

        var instruments = CandidateValidationService.Validate(extracted, occurredAt);

        if (instruments.Count == 0)
        {
            // Métrica do doc 10: quantas vezes o funil determinístico salvou o sistema de uma
            // extração errada. Não deve ser zero — zero significa que o funil não está sendo
            // exercitado, e provavelmente que a métrica está errada.
            if (extracted.HasCandidates)
            {
                logger.LogInformation(
                    "Extração por IA propôs candidatos e NENHUM sobreviveu à validação determinística.");
            }

            return null;
        }

        return ExtractionResult.Found(instruments, ExtractionMethod.Vision);
    }

    /// <summary>
    /// O que o sistema já sabe, para reduzir alucinação em campo cortado.
    /// </summary>
    /// <remarks>
    /// Só dado do próprio tenant sai daqui — documentos do <c>PayerProfile</c> dele e nomes de
    /// beneficiários que ele cadastrou. Nada de outro tenant, nem o conteúdo da caixa.
    /// </remarks>
    private async Task<ExtractionHints> BuildHintsAsync(
        TenantId tenantId,
        PayerProfile? profile,
        string? sender,
        CancellationToken cancellationToken)
    {
        var taxIds = profile is null
            ? []
            : new[] { profile.PrimaryTaxId.Value }
                .Concat(profile.AdditionalTaxIds.Select(t => t.Value))
                .ToList();

        var knownPayees = await payees.ListByTenantAsync(tenantId, cancellationToken);

        return ExtractionHints.From(
            taxIds,
            knownPayees.Select(p => p.LegalName),
            sender);
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
        => new(Guid.Empty, string.Empty, 0, null, null);
}
