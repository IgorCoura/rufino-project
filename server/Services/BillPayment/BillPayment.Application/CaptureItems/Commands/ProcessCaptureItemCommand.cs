namespace BillPayment.Application.CaptureItems.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;
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
/// <param name="VisionLane">
/// Quem chama é o worker de visão, e portanto o degrau 3 pode rodar.
/// <para>
/// <strong>A extração por IA NÃO acontece na faixa rápida</strong>, e é essa regra que sustenta a
/// vazão do processamento. A cota é escassa e a chamada leva de 3 a 5 segundos; um item de visão
/// no meio do lote segurava todos os outros, cuja mediana é de 150 ms — medido em 2026-08-26: 27%
/// dos itens consumindo 86% do tempo. A faixa rápida faz os degraus 0 a 2 e, quando precisa da IA,
/// põe o item em <c>VisionPending</c> e segue. O worker de visão retoma dali.
/// </para>
/// <para>
/// O custo de ceder a vez é rebaixar o artefato uma vez — os mesmos 150 a 360 ms —, contra os
/// segundos que ele deixa de bloquear.
/// </para>
/// </param>
public sealed record ProcessCaptureItemCommand(Guid TenantId, Guid CaptureItemId, bool VisionLane = false)
    : ITenantScopedCommand, IRequest<ProcessCaptureItemResponse>, IMultiAggregateCommand;

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
    ICapturedMessageRepository capturedMessages,
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

    /// <summary>Motivo enquanto o artefato espera a vez na fila do extrator de IA.</summary>
    private const string AWAITING_VISION = "awaiting_vision";

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

        // Anexo manual não se rebaixa do provedor: uma pessoa já buscou o documento e o entregou,
        // e ir ao e-mail de novo traria de volta o corpo que NÃO tinha o boleto — desfazendo o
        // trabalho dela. É o único caminho em que o artefato não vem da caixa.
        ReadOnlyMemory<byte>? content = item.ManuallySupplied && item.HasStoredArtifact
            ? await storage.RetrieveAsync(tenantId, item.StorageKey!, cancellationToken)
            : await mailboxReader.DownloadArtifactAsync(
                source.Address, source.Credential!, item.ExternalMessageId, item.ArtifactKey, cancellationToken);

        // O endereço guardado pode ter morrido — mover a mensagem de pasta o invalida. Antes de
        // desistir, reencontra a mensagem pelo identificador do cabeçalho, que não muda nunca.
        if (content is null && !item.ManuallySupplied)
            content = await RetryAfterRelocationAsync(item, source, now.UtcDateTime, cancellationToken);

        // Vazio conta como ausente: um adapter que devolve zero byte nao entregou o artefato,
        // e seguir com ele faria a cascata concluir "nao e boleto" sobre um nada.
        if (content is null || content.Value.IsEmpty)
        {
            // Anexo que não veio não é "não é boleto": nada se aprendeu sobre ele, e descartar
            // perderia um documento que a próxima tentativa traria.
            item.MarkLinkFailed("artifact_download_failed", now.UtcDateTime);

            await RecordCapturedOutcomeAsync(
                item, ArtifactOutcome.DownloadFailed, "artifact_download_failed",
                tenantId, now.UtcDateTime, cancellationToken);

            await unitOfWork.SaveEntitiesAsync(cancellationToken);

            return new ProcessCaptureItemResponse(item.Id.Value, "DownloadFailed", 0);
        }

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);
        var passwords = PasswordDerivationService.Derive(profile);

        // Os documentos do cadastro vão junto para a varredura procurá-los DIRETAMENTE no texto,
        // em vez de descobrir números parecidos com documento e conferir depois. Medido em 915
        // boletos reais: +54 documentos encontrados, nenhuma perda, zero falso positivo.
        var knownTaxIds = KnownTaxIdsOf(profile);

        var extraction = await parser.ParseAsync(
            content.Value,
            item.ContentType,
            passwords,
            knownTaxIds,
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
        // A escada de link não roda sobre anexo manual: o documento já está em mãos, e buscar de
        // novo gastaria rede — e abriria superfície de ataque — para descobrir o que já se tem.
        if (!extraction.Resolved && !extraction.IsLocked && !item.ManuallySupplied)
        {
            var resolved = await linkResolver.ResolveAsync(payload, payloadType, cancellationToken);

            if (resolved is null)
            {
                // A escada não alcançou este emissor. Guardar PARA ONDE ela teria ido é o que
                // transforma a quarentena em fila de receitas a cadastrar — sem isto o item cai
                // lá sem dizer de onde veio, que é justamente a informação que faltava.
                RecordAttemptedLinkAsync(item, payload, payloadType, now.UtcDateTime);
            }
            else
            {
                // A procedência é registrada mesmo que o documento buscado não resolva: saber
                // ONDE o sistema foi procurar é o que permite corrigir a receita depois.
                item.RecordResolvedLink(resolved.SourceUrl, now.UtcDateTime);

                payload = resolved.Content;
                payloadType = resolved.MediaType;

                extraction = await parser.ParseAsync(
                    payload, payloadType, passwords, knownTaxIds,
                    DateOnly.FromDateTime(now.UtcDateTime), cancellationToken);
            }
        }

        // A IA roda para TODO candidato a boleto (decisão de 2026-08-27): o que o determinístico
        // resolveu ganha o retrato de enriquecimento (competência, descrição, pagador), e o que
        // não resolveu ganha o degrau 3 como sempre. PDF cifrado continua fora — mandar um
        // arquivo que não abre gastaria a chamada para o modelo ver a tela de senha.
        DocumentReading? reading = null;

        if (!extraction.IsLocked && ShouldUseVision(item, origin, payloadType, extraction))
        {
            // Na faixa rápida a IA não roda: o item cede o lugar e o worker de visão o retoma.
            if (!request.VisionLane)
            {
                item.MarkVisionPending(AWAITING_VISION, now.UtcDateTime);
                await unitOfWork.SaveEntitiesAsync(cancellationToken);

                return new ProcessCaptureItemResponse(item.Id.Value, "VisionPending", 0);
            }

            // O PDF que abriu por senha derivada abriu SÓ AQUI DENTRO: os bytes continuam
            // cifrados, e mandá-los assim faz o extrator recusar o artefato. Medido em
            // 2026-08-28 — os três únicos boletos do acervo sem retrato eram exatamente os três
            // com senha derivada, e o log trazia um 400 do provedor para cada um.
            var visionPayload = payload;
            var readable = true;

            if (extraction.UnlockedBy is not null)
            {
                var clear = await parser.UnlockAsync(payload, payloadType, passwords, cancellationToken);

                if (clear is { } unlocked)
                    visionPayload = unlocked;
                else
                    readable = false;
            }

            // Sem cópia legível, a regra de sempre volta a valer e o item apenas segue sem
            // retrato: PDF que não abre não vai para o extrator. O que a cascata determinística
            // já provou continua valendo — a captura nunca é refém da IA.
            if (readable)
            {
                (extraction, reading) = await ExtractWithVisionAsync(
                    item, tenantId, profile, extraction, visionPayload, payloadType, now.UtcDateTime, cancellationToken);
            }
        }

        // Assunto e remetente entram como evidência fraca: sem eles, "não achei boleto" de um
        // remetente não cadastrado é sempre descarte, e emissor novo desaparece em silêncio.
        var decision = CaptureTriageService.Decide(extraction, origin, item.Subject);

        // A escada decide ANTES de qualquer coisa ser guardada: só roda sobre o que a cascata
        // reconheceu como boleto, e o documento de outro pagador é descartado como qualquer
        // não-boleto — sem arquivo no balde, sem item, só a linha do livro-caixa. É a regra de
        // isolamento fechada em 2026-08-28: o que não é deste tenant some, e ninguém fica sabendo
        // de quem era (ADR-008, revisado).
        var routing = decision == CaptureTriageDecision.Parse
            ? await DecideRouteAsync(extraction, profile, tenantId, cancellationToken)
            : null;

        var isForeign = routing?.Outcome == RoutingOutcome.Foreign;
        if (isForeign)
            decision = CaptureTriageDecision.Drop;

        await ApplyAsync(
            item, decision, extraction, payload, payloadType, tenantId, now.UtcDateTime, cancellationToken);

        if (decision == CaptureTriageDecision.Parse)
            await ApplyRoutingAsync(item, extraction, reading, routing!, tenantId, now.UtcDateTime, cancellationToken);

        await RecordCapturedOutcomeAsync(
            item,
            OutcomeOf(decision, item),
            isForeign ? routing!.Reason : item.Reason,
            tenantId,
            now.UtcDateTime,
            cancellationToken);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ProcessCaptureItemResponse(
            item.Id.Value,
            decision.Name,
            extraction.Instruments.Count,
            routing?.Outcome.Name,
            item.BillId?.Value);
    }


    /// <summary>
    /// Tenta de novo depois de reencontrar a mensagem pelo identificador permanente do cabeçalho.
    /// </summary>
    /// <remarks>
    /// <strong>Um 404 no anexo raramente significa "o arquivo sumiu".</strong> O id guardado é o
    /// endereço de onde a mensagem estava, e mover de pasta o invalida — medido em produção em
    /// 2026-08-19, com 2.381 downloads bem-sucedidos e 6 falhas, todas assim. Sem esta segunda
    /// tentativa, reprocessar refaz a mesma requisição morta e falha igual, para sempre.
    /// </remarks>
    private async Task<ReadOnlyMemory<byte>?> RetryAfterRelocationAsync(
        CaptureItem item,
        CaptureSource source,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(item.InternetMessageId))
            return null;

        var relocated = await mailboxReader.RelocateArtifactAsync(
            source.Address, source.Credential!, item.InternetMessageId, item.FileName, cancellationToken);

        if (relocated is null)
            return null;

        // Regravar os ids é o que impede a próxima chamada de repetir a busca — e o que faz o
        // botão de reprocessar voltar a significar alguma coisa.
        item.Relocate(relocated.ExternalMessageId, relocated.ArtifactKey, occurredAt);

        logger.LogInformation("Mensagem reencontrada pelo identificador do cabeçalho; ids atualizados.");

        return await mailboxReader.DownloadArtifactAsync(
            source.Address, source.Credential!, item.ExternalMessageId, item.ArtifactKey, cancellationToken);
    }

    /// <summary>
    /// Escreve no livro-caixa da captura o que aconteceu com este artefato.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É o único lugar onde o descarte deixa rastro.</strong> O <c>CaptureItem</c> some
    /// quando a triagem descarta — é a retenção por desfecho —, e sem este registro a pessoa que
    /// mandou o e-mail fica sem resposta sobre o que houve com ele.
    /// </para>
    /// <para>
    /// Ausência do registro não derruba o processamento: item ingerido antes de o livro-caixa
    /// existir continua sendo processado normalmente, só não aparece na tela de e-mails.
    /// </para>
    /// </remarks>
    private async Task RecordCapturedOutcomeAsync(
        CaptureItem item,
        ArtifactOutcome outcome,
        string? reason,
        TenantId tenantId,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var message = await capturedMessages.FindByExternalMessageIdAsync(
            tenantId, item.SourceId, item.ExternalMessageId, cancellationToken);

        if (message is null)
            return;

        // Descartado não tem para onde navegar: o item foi apagado, e apontar para um id morto
        // faria a tela oferecer um link que devolve 404.
        var captureItemId = outcome == ArtifactOutcome.Discarded ? (CaptureItemId?)null : item.Id;

        message.RecordOutcome(item.ArtifactKey, outcome, reason, captureItemId, item.BillId, occurredAt);
    }

    /// <summary>Traduz o desfecho técnico para a linguagem de quem lê a tela.</summary>
    private static ArtifactOutcome OutcomeOf(CaptureTriageDecision decision, CaptureItem item)
    {
        if (decision == CaptureTriageDecision.Drop)
            return ArtifactOutcome.Discarded;

        if (item.Status == CaptureItemStatus.Promoted) return ArtifactOutcome.Promoted;
        if (item.Status == CaptureItemStatus.ForeignPayer) return ArtifactOutcome.ForeignPayer;
        if (item.Status == CaptureItemStatus.Unrouted) return ArtifactOutcome.Unrouted;
        if (item.Status == CaptureItemStatus.Locked) return ArtifactOutcome.Locked;
        if (item.Status == CaptureItemStatus.Unrecognized) return ArtifactOutcome.Quarantined;
        if (item.Status == CaptureItemStatus.LinkFailed) return ArtifactOutcome.DownloadFailed;

        return ArtifactOutcome.Pending;
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
    private async Task<RoutingDecision> DecideRouteAsync(
        ExtractionResult extraction,
        PayerProfile? profile,
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        var exclusive = await ResolveExclusivePayeesAsync(tenantId, extraction, cancellationToken);
        return BillRoutingService.Route(extraction, profile, exclusive);
    }

    /// <summary>
    /// Aplica o desfecho da escada ao item já guardado. <c>Foreign</c> nunca chega aqui — virou
    /// descarte antes de o arquivo ir para o balde.
    /// </summary>
    private async Task ApplyRoutingAsync(
        CaptureItem item,
        ExtractionResult extraction,
        DocumentReading? reading,
        RoutingDecision routing,
        TenantId tenantId,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        if (routing.Outcome == RoutingOutcome.Unrouted)
        {
            item.MarkUnrouted(routing.Reason, occurredAt);
            return;
        }

        await PromoteAsync(item, extraction, reading, routing, tenantId, occurredAt, cancellationToken);
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
        DocumentReading? reading,
        RoutingDecision routing,
        TenantId tenantId,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        // O nome do pagador vem da leitura quando ela leu o MESMO documento que roteou — nome de
        // um e documento de outro descreveria uma pessoa que não existe.
        var payerName = reading?.PayerTaxId is { } readTaxId && readTaxId.Equals(routing.PayerTaxId)
            ? reading.PayerName
            : null;

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
            routing.PayerTaxId is null ? null : PartyInfo.Of(payerName, routing.PayerTaxId),
            routing.Confidence,
            reading);

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
            // misto, e é o que mantém a fila de quarentena utilizável por uma pessoa. Desde
            // 2026-08-28 é também o desfecho do boleto de OUTRO pagador: o registro do livro-caixa
            // fica com o motivo, e o documento nunca chega ao balde.
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
            item.StoreArtifact(Sha256Of(content.Span), CaptureItem.PENDING_UNLOCK, occurredAt);
            item.MarkLocked(occurredAt);
            return;
        }

        // Unrecognized, e nao Unrouted: o parser nao reconheceu boleto, que e coisa diferente
        // de nao saber de quem e o boleto. E e de Unrecognized que sai o caminho previsto pelo
        // doc 09 — a pessoa informa a linha digitavel a mao e o item volta para Parsed.
        item.StoreArtifact(Sha256Of(content.Span), CaptureItem.PENDING_REVIEW, occurredAt);
        item.MarkUnrecognized(extraction.ReasonCode ?? "no_instrument", occurredAt);

        logger.LogInformation(
            "Item de remetente cadastrado ficou em quarentena sem instrumento reconhecido.");
    }

    /// <summary>
    /// A leitura por IA do artefato — degrau 3 quando o determinístico não resolveu, e retrato de
    /// enriquecimento sempre.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>O que volta do modelo não é boleto: é string.</strong> Quem converte é o
    /// <c>CandidateValidationService</c>, e ele só aceita o que sobrevive ao DV da linha
    /// digitável ou ao CRC do BR Code (ADR-011). Lista vazia depois de o modelo ter respondido é
    /// desfecho normal — inclusive quando ele alucinou, e é essa a defesa.
    /// </para>
    /// <para>
    /// <strong>O corpo do e-mail vai junto</strong> — é dele que saem a competência e a descrição
    /// quando o boleto não as traz. E <strong>a captura nunca é refém da IA</strong>: modelo
    /// indisponível ou teto estourado devolvem a extração determinística intacta, sem retrato.
    /// </para>
    /// </remarks>
    private async Task<(ExtractionResult Extraction, DocumentReading? Reading)> ExtractWithVisionAsync(
        CaptureItem item,
        TenantId tenantId,
        PayerProfile? profile,
        ExtractionResult extraction,
        ReadOnlyMemory<byte> content,
        string? contentType,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var hints = await BuildHintsAsync(tenantId, profile, item.Sender, cancellationToken);
        var body = await LoadBodyTextAsync(item, tenantId, cancellationToken);

        var attempt = await documentIntelligence.ExtractAsync(
            DocumentPayload.From(tenantId, content, contentType, body?.Text, body?.IsHtml ?? false),
            hints,
            cancellationToken);

        // O provedor não respondeu: NADA foi aprendido sobre o documento, e concluir "não é
        // boleto" a partir disso é o defeito medido em 2026-08-27 — 24 documentos bons foram
        // para a quarentena por 503. Lançar devolve o item à fila, onde a máquina de
        // retentativa com espera dobrando já existe e agora finalmente é acionada.
        if (attempt.IsRetryable)
            throw ExtractionErrors.ProviderUnavailable(attempt.ReasonCode ?? attempt.Status.Name);

        var extracted = attempt.Document;

        var candidate = DocumentReading.FromExtraction(
            extracted, new DateTimeOffset(occurredAt, TimeSpan.Zero));
        var reading = candidate.HasContent ? candidate : null;

        if (extraction.Resolved)
            return (extraction, reading);

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

            return (extraction, reading);
        }

        // O documento do pagador lido pela visão entra na escada de roteamento — é o que faz um
        // documento ESCANEADO subir ao degrau 1 em vez de cair na reivindicação. Entra SEM o
        // rótulo de pagador, de propósito: o rótulo é o que autoriza o degrau negativo, e o
        // modelo devolve em payerTaxId o CNPJ do beneficiário impresso com frequência suficiente
        // (DV válido por construção) para que "é de outra pessoa" decidido por ele fosse perder a
        // conta do tenant sem caminho de volta. Sem rótulo, o documento do tenant ainda promove
        // (degrau 1 casa com o cadastro) e o de terceiro vai para a reivindicação, onde uma
        // pessoa decide (auditoria 2026-08-28, ADR-011 estendido à posse do boleto).
        var parties = PartyCandidate.TryCreate(extracted.PayerTaxId, underPayerLabel: false) is { } party
            ? new[] { party }
            : [];

        return (ExtractionResult.Found(instruments, ExtractionMethod.Vision, parties: parties), reading);
    }

    /// <summary>
    /// O corpo do e-mail que trouxe este artefato, como texto — do balde onde a sincronização o
    /// guardou. Ausência é estado normal (mensagem anterior à retenção do corpo, ou upload manual).
    /// </summary>
    private async Task<(string Text, bool IsHtml)?> LoadBodyTextAsync(
        CaptureItem item,
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        if (item.ManuallySupplied)
            return null;

        var message = await capturedMessages.FindByExternalMessageIdAsync(
            tenantId, item.SourceId, item.ExternalMessageId, cancellationToken);

        if (message is null || !message.HasStoredBody)
            return null;

        var stored = await storage.RetrieveAsync(tenantId, message.BodyStorageKey!, cancellationToken);
        if (stored.IsEmpty)
            return null;

        var isHtml = message.BodyContentType?.Contains("html", StringComparison.OrdinalIgnoreCase) ?? true;
        return (System.Text.Encoding.UTF8.GetString(stored.Span), isHtml);
    }

    /// <summary>
    /// A extração por IA se aplica a este artefato?
    /// </summary>
    /// <remarks>
    /// <para>
    /// Existe separada da chamada porque a <strong>faixa rápida precisa da resposta sem gastar
    /// nada</strong>: é ela que decide entre seguir o processamento e ceder a vez ao worker de
    /// visão. Colar as duas coisas obrigaria a faixa rápida a chamar a IA para descobrir se
    /// deveria chamá-la.
    /// </para>
    /// <para>
    /// <strong>Resolvido pelo determinístico é candidato por definição</strong> — tem boleto, e
    /// todo boleto ganha o retrato da IA (decisão de 2026-08-27). O portão de sinal de cobrança
    /// segue valendo só para o que NÃO resolveu: é ele que impede holerite e nota fiscal de
    /// queimarem chamada.
    /// </para>
    /// <para>
    /// O tipo é o <strong>declarado na ingestão</strong>, nunca deduzido do nome: a chave do
    /// artefato é opaca no provedor, e adivinhar dali rotulava toda imagem como PDF. E o portão
    /// examina <c>FileName</c>, não a chave, pelo mesmo motivo — sinal de cobrança nunca casaria
    /// com um identificador opaco.
    /// </para>
    /// </remarks>
    private bool ShouldUseVision(
        CaptureItem item,
        TrustedOrigin? origin,
        string? contentType,
        ExtractionResult extraction)
        => documentIntelligence.IsEnabled
            && DocumentPayload.IsSupported(contentType)
            && (extraction.Resolved || VisionGateService.ShouldAttempt(origin, item.Subject, item.FileName));

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

    /// <summary>
    /// Os documentos fiscais que o tenant declarou ter — principal e adicionais.
    /// </summary>
    /// <remarks>
    /// Sem perfil a lista é vazia, e a varredura cai só no degrau genérico. É o mesmo estado em
    /// que <c>PasswordDerivationService</c> não produz candidata nenhuma: sem cadastro o sistema
    /// não tem contra o que comparar.
    /// </remarks>
    private static IReadOnlyList<TaxId> KnownTaxIdsOf(PayerProfile? profile)
        => profile is null
            ? []
            : [profile.PrimaryTaxId, .. profile.AdditionalTaxIds];

    /// <summary>Guarda o endereço que a escada tentaria, quando não há receita para ele.</summary>
    /// <remarks>
    /// Só o primeiro link entra: um e-mail traz dezenas, quase todos rastreador e rodapé, e o
    /// campo existe para identificar o emissor — não para inventariar a mensagem.
    /// </remarks>
    private void RecordAttemptedLinkAsync(
        CaptureItem item,
        ReadOnlyMemory<byte> payload,
        string? payloadType,
        DateTime occurredAt)
    {
        if (item.SourceUrl is not null)
            return;

        var candidate = linkResolver.HarvestLinks(payload, payloadType).FirstOrDefault();

        if (candidate is null || candidate.Url.Length > CaptureItem.SOURCE_URL_MAX_LENGTH)
            return;

        item.RecordAttemptedLink(candidate.Url, occurredAt);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Nenhuma receita de link para {LinkHost}; o artefato segue para triagem sem o documento.",
                candidate.Host);
        }
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
