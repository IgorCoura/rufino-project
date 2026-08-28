namespace BillPayment.Application.CaptureSources.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Mailboxes;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Varre uma fonte e ingere o que apareceu desde o último cursor.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Marcado como <see cref="IMultiAggregateCommand"/>, e é o primeiro do BC.</strong> A
/// justificativa é a atomicidade entre o cursor e os itens: se a <c>CaptureSource</c> avançasse
/// numa transação e os <c>CaptureItem</c> nascessem noutra, uma falha no meio produziria
/// <em>boletos perdidos</em> (cursor à frente dos itens) ou <em>ingestão repetida</em> (itens sem
/// cursor). Eventual consistency por Domain Event não resolve: o cursor é a única prova de até
/// onde a caixa foi lida, e ele não pode ficar "quase" certo.
/// </para>
/// <para>
/// Como há exatamente um <c>SaveEntitiesAsync</c>, a transação implícita do EF já cobre tudo — o
/// marker é documentação greppável, não muda o pipeline.
/// </para>
/// <para>
/// <strong>Uma fonte por comando, de propósito.</strong> O agendador chama este comando N vezes,
/// uma por fonte, cada uma na sua transação — do mesmo modo que o outbox reivindica uma mensagem
/// por vez. Uma caixa fora do ar não pode impedir as outras de sincronizar.
/// </para>
/// </remarks>
public sealed record SyncCaptureSourceCommand(Guid TenantId, Guid CaptureSourceId)
    : IRequest<SyncCaptureSourceResponse>, IMultiAggregateCommand;

/// <param name="Status">O desfecho da conversa com o provedor — <c>Ok</c>, <c>Denied</c>, <c>CursorExpired</c> ou <c>Unavailable</c>.</param>
/// <param name="IngestedItems">Quantos artefatos novos entraram. Zero é desfecho normal.</param>
/// <param name="SkippedAsAlreadyIngested">Quantos já existiam — a prova de que reprocessar não duplica.</param>
/// <param name="HasMorePages">
/// Alguma pasta parou no teto de páginas em vez de chegar ao fim da caixa. É o sinal para o
/// agendador emendar a varredura seguinte em vez de dormir o intervalo inteiro sobre trabalho
/// pendente: a enumeração do provedor vai do mais antigo para o mais novo, então numa caixa
/// grande a mensagem recém-chegada está no FIM.
/// </param>
public sealed record SyncCaptureSourceResponse(
    Guid Id,
    string Status,
    int IngestedItems,
    int SkippedAsAlreadyIngested,
    bool HasMorePages = false);

public sealed class SyncCaptureSourceCommandHandler(
    ICaptureSourceRepository sources,
    ICaptureItemRepository items,
    ICapturedMessageRepository capturedMessages,
    IMailboxReader mailboxReader,
    IAttachmentStorage storage,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SyncCaptureSourceCommand, SyncCaptureSourceResponse>
{
    public async Task<SyncCaptureSourceResponse> Handle(
        SyncCaptureSourceCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var sourceId = CaptureSourceId.From(request.CaptureSourceId);

        var source = await sources.GetAsync(tenantId, sourceId, cancellationToken)
            ?? throw CaptureSourceErrors.NotFound(request.CaptureSourceId);

        // Escolha de porta pelo Kind é orquestração: não ter o que varrer não é falha, e
        // registrá-la como tal encheria a tela de erro que ninguém pode resolver.
        if (!source.Kind.SupportsIncrementalSync)
            return new SyncCaptureSourceResponse(source.Id.Value, "NotApplicable", 0, 0);

        // Recusa fonte desativada (BLP.CPS12) e devolve as pastas a varrer, numa chamada só.
        var folders = source.BeginSync();

        var now = clock.GetUtcNow();
        var ingested = 0;
        var skipped = 0;
        var hasMorePages = false;
        var statuses = new List<MailboxStatus>(folders.Count);

        // Uma pasta por vez, cada uma com o próprio cursor e o próprio desfecho. Uma pasta
        // renomeada no cliente de e-mail registra a falha dela e NÃO impede as outras de
        // sincronizar — mesma disciplina que faz uma caixa fora do ar não travar as demais.
        foreach (var folder in folders)
        {
            // O piso é da fonte e vale para toda pasta. Ele só surte efeito quando não há cursor:
            // o provedor grava o filtro dentro do deltaLink, e quem descarta o cursor ao trocar a
            // data é CaptureSource.ChangeCaptureSince.
            var result = await mailboxReader.ReadAsync(
                source.Address,
                source.Credential!,
                folder.Path,
                folder.SyncCursor,
                source.CaptureSince,
                cancellationToken);

            if (result.IsOk)
            {
                var (folderIngested, folderSkipped) =
                    await IngestAsync(source, result, now.UtcDateTime, cancellationToken);

                ingested += folderIngested;
                skipped += folderSkipped;
            }

            hasMorePages |= result.HasMorePages;

            RecordOutcome(source, folder.Id, result, now.UtcDateTime);
            statuses.Add(result.Status);
        }

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new SyncCaptureSourceResponse(
            source.Id.Value, Summarize(statuses), ingested, skipped, hasMorePages);
    }

    /// <summary>
    /// Cria um <c>CaptureItem</c> por artefato, pulando o que esta fonte já ingeriu.
    /// </summary>
    /// <remarks>
    /// A checagem prévia evita o round-trip virar violação de índice único no <c>SaveChanges</c>,
    /// que derrubaria a varredura inteira por causa de uma mensagem repetida. O índice continua
    /// sendo quem garante sob concorrência — isto é o caminho comum, não a garantia.
    /// </remarks>
    private async Task<(int Ingested, int Skipped)> IngestAsync(
        CaptureSource source,
        MailboxReadResult result,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var ingested = 0;
        var skipped = 0;

        // O livro-caixa nasce por MENSAGEM, antes dos itens: é ele que sobrevive ao descarte, e
        // registrar depois deixaria de fora justamente o e-mail que não virou item nenhum.
        foreach (var message in result.Messages)
            await RegisterCapturedMessageAsync(source, message, occurredAt, cancellationToken);

        // Um item por artefato, não por mensagem: um e-mail com três boletos vira três itens.
        var artifacts = result.Messages.SelectMany(
            message => message.Artifacts,
            (message, artifact) => (Message: message, Artifact: artifact));

        foreach (var (message, artifact) in artifacts)
        {
            var alreadyIngested = await items.ExistsAsync(
                source.TenantId, source.Id, message.MessageId, artifact.Key, cancellationToken);

            if (alreadyIngested)
            {
                skipped++;
                continue;
            }

            var item = CaptureItem.Ingest(
                source.TenantId,
                source.Id,
                message.MessageId,
                artifact.Key,
                message.Sender,
                message.Subject,
                message.ReceivedAt.UtcDateTime,
                occurredAt,

                // O tipo declarado tem que ser guardado AGORA: a chave do artefato é opaca no
                // provedor e o processamento não teria de onde deduzi-lo depois.
                artifact.ContentType,
                artifact.FileName,
                message.InternetMessageId);

            await items.AddAsync(item, cancellationToken);
            ingested++;
        }

        return (ingested, skipped);
    }

    /// <summary>
    /// Registra a mensagem no livro-caixa da captura, se ela ainda não estiver lá.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Existe para o e-mail que não vira nada.</strong> A triagem descarta o que não é
    /// boleto e o item some — decisão medida, porque 250 de 404 anexos de uma caixa real não
    /// tinham sinal nenhum de cobrança e encheriam a quarentena. O preço era ninguém conseguir
    /// dizer o que houve com um e-mail; este registro é a resposta, e guarda metadado, nunca o
    /// arquivo.
    /// </para>
    /// <para>
    /// Idempotente por <c>(tenant, fonte, mensagem)</c>, como a ingestão dos itens: reler a caixa
    /// não duplica linha nenhuma.
    /// </para>
    /// </remarks>
    private async Task RegisterCapturedMessageAsync(
        CaptureSource source,
        MailboxMessage message,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var existing = await capturedMessages.FindByExternalMessageIdAsync(
            source.TenantId, source.Id, message.MessageId, cancellationToken);

        if (existing is not null)
            return;

        var captured = CapturedMessage.Register(
            source.TenantId,
            source.Id,
            message.MessageId,
            message.Sender,
            message.Subject,
            message.ReceivedAt.UtcDateTime,
            occurredAt,
            message.Artifacts.Select(a => (a.Key, a.FileName, a.ContentType)),
            message.InternetMessageId);

        await StoreBodyAsync(captured, source, message, occurredAt, cancellationToken);

        await capturedMessages.AddAsync(captured, cancellationToken);
    }

    /// <summary>
    /// Guarda o corpo do e-mail no balde, cifrado, e registra a chave no livro-caixa.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É o que faz "ver o e-mail" existir</strong> — e é também a entrada de corpo da
    /// extração por IA. O corpo segue a doutrina do adapter (não viaja na listagem): a
    /// sincronização o baixa por chamada dedicada, uma vez por mensagem NOVA, e o que fica é o
    /// objeto cifrado no balde, atrás do mesmo portão do documento original (ADR-008).
    /// </para>
    /// <para>
    /// <strong>Corpo que não veio não derruba a varredura.</strong> O registro fica sem corpo e a
    /// tela cai no plano B — rebuscar no provedor na hora de mostrar.
    /// </para>
    /// </remarks>
    private async Task StoreBodyAsync(
        CapturedMessage captured,
        CaptureSource source,
        MailboxMessage message,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        var body = await mailboxReader.DownloadArtifactAsync(
            source.Address, source.Credential!, message.MessageId, IMailboxReader.BODY_ARTIFACT_KEY, cancellationToken);

        if (body is null || body.Value.IsEmpty)
            return;

        var contentType = message.Artifacts
            .FirstOrDefault(a => string.Equals(a.Key, IMailboxReader.BODY_ARTIFACT_KEY, StringComparison.Ordinal))
            ?.ContentType ?? "text/html";

        var storageKey = await storage.StoreAsync(
            source.TenantId,
            $"message-body-{captured.Id.Value:N}",
            contentType,
            body.Value,
            cancellationToken);

        captured.RecordBody(storageKey, contentType, occurredAt);
    }

    /// <summary>
    /// Traduz o desfecho da leitura para o agregado. Quem sabe o que cada status significa para o
    /// cursor é o próprio <c>MailboxStatus</c> — o handler só encaminha.
    /// </summary>
    private static void RecordOutcome(
        CaptureSource source,
        MonitoredFolderId folderId,
        MailboxReadResult result,
        DateTime occurredAt)
    {
        if (result.IsOk)
        {
            source.RecordSyncSuccess(folderId, result.NextCursor, occurredAt);
            return;
        }

        // Cursor invalidado pelo provedor: descartar é a resposta, e sem isso a pasta pararia de
        // sincronizar em silêncio. A falha continua registrada para o usuário ver o que houve.
        if (result.RequiresCursorReset)
            source.ResetCursor(folderId, occurredAt);

        source.RecordSyncFailure(folderId, result.ReasonCode!, occurredAt);
    }

    /// <summary>
    /// Um desfecho só para a resposta, a partir dos desfechos de cada pasta.
    /// </summary>
    /// <remarks>
    /// <strong>A falha vence o êxito.</strong> Devolver <c>Ok</c> porque a maioria das pastas foi
    /// bem esconderia a que não foi — e quem chamou o endereço manualmente está justamente
    /// conferindo se a conexão funciona. O detalhe por pasta está na consulta da fonte.
    /// </remarks>
    private static string Summarize(List<MailboxStatus> statuses)
    {
        if (statuses.Count == 0)
            return "NotApplicable";

        var failure = statuses.Find(s => s != MailboxStatus.Ok);
        return (failure ?? MailboxStatus.Ok).Name;
    }
}

public sealed class SyncCaptureSourceIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<SyncCaptureSourceIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<SyncCaptureSourceCommand, SyncCaptureSourceResponse>(mediator, requestManager, logger)
{
    protected override SyncCaptureSourceResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, 0, 0);
}
