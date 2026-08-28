namespace BillPayment.Application.CapturedMessages.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Mailboxes;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Puxa o e-mail de novo do provedor e o faz passar pela triagem inteira outra vez.
/// </summary>
/// <remarks>
/// <para>
/// Existe para desfazer erro de triagem ou de download: a cascata ganhou degrau, o cadastro
/// mudou, o anexo veio pela metade. <strong>Refaz do zero</strong> — e por isso a única
/// proibição é o boleto que já teve o pagamento autorizado (aprovado, agendado, tentado ou
/// pago): esse não se refaz por trás de quem decidiu (<c>BLP.CMS11</c>). Boleto ainda não
/// decidido é cancelado para a triagem nova recriá-lo; boleto negado não bloqueia, mas volta na
/// resposta como aviso. Quem aplica essa regra é o <c>MessageRecaptureService</c>.
/// </para>
/// <para>
/// <strong>O registro e os itens são reescritos em cima do que existe</strong> — mesmo id, mesma
/// URL. A versão até 2026-08-28 apagava e recriava: devolvia um id que não existia mais, apagava
/// do balde o documento de boleto já promovido, e corria contra o índice único dos itens.
/// </para>
/// <para>
/// <strong>A ordem é: buscar no provedor, decidir, mutar, commitar, e só então apagar blobs.</strong>
/// Falhar em qualquer passo antes do commit não deixa rastro; apagar antes do commit deixaria
/// linha apontando para arquivo inexistente se a transação não fechasse. Blob de item que
/// pertencia a um boleto (mesmo cancelado) NÃO é apagado — é a evidência do boleto.
/// </para>
/// </remarks>
public sealed record RecaptureMessageCommand(Guid TenantId, Guid CapturedMessageId, Guid RequestedBy)
    : ITenantScopedCommand, IRequest<RecaptureMessageResponse>, IMultiAggregateCommand;

/// <summary>
/// <paramref name="PreviouslyDeniedBillIds"/> é o aviso: esses boletos já tinham sido negados
/// uma vez, e vão renascer para decisão de novo.
/// </summary>
public sealed record RecaptureMessageResponse(
    Guid Id,
    int ArtifactsReingested,
    int BillsCancelled,
    IReadOnlyList<Guid> PreviouslyDeniedBillIds);

public sealed class RecaptureMessageCommandHandler(
    ICapturedMessageRepository capturedMessages,
    ICaptureItemRepository items,
    IBillRepository bills,
    ICaptureSourceRepository sources,
    IMailboxReader mailboxReader,
    IAttachmentStorage storage,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<RecaptureMessageCommandHandler> logger)
    : IRequestHandler<RecaptureMessageCommand, RecaptureMessageResponse>
{
    private const string CANCELLATION_REASON = "Recaptura do e-mail de origem.";

    public async Task<RecaptureMessageResponse> Handle(
        RecaptureMessageCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var id = CapturedMessageId.From(request.CapturedMessageId);
        var requestedBy = UserId.From(request.RequestedBy);

        var message = await capturedMessages.GetAsync(tenantId, id, cancellationToken)
            ?? throw CapturedMessageErrors.NotFound(request.CapturedMessageId);

        message.EnsureCanBeRecaptured();

        var source = await sources.GetAsync(tenantId, message.SourceId, cancellationToken)
            ?? throw CaptureSourceErrors.NotFound(message.SourceId.Value);

        // Passo 1: o provedor primeiro. Se a mensagem não existe mais, nada foi tocado.
        var read = await FetchFromProviderAsync(source, message, request.CapturedMessageId, cancellationToken);

        // Passo 2: o que existe hoje, e o que a recaptura pode desfazer.
        var existing = await items.ListByMessageAsync(tenantId, source.Id, message.ExternalMessageId, cancellationToken);
        var linked = await LinkBillsAsync(tenantId, existing, cancellationToken);
        var plan = MessageRecaptureService.Plan(message, linked);

        var now = clock.GetUtcNow().UtcDateTime;

        foreach (var bill in plan.BillsToCancel)
            bill.Cancel(requestedBy, CANCELLATION_REASON, now);

        // Passo 3: reescrever em cima do que existe. As chaves antigas são só anotadas — nada sai
        // do balde antes do commit.
        var orphanedKeys = new List<string>();

        var previousBodyKey = message.Recapture(
            read.MessageId,
            read.Sender,
            read.Subject,
            read.ReceivedAt.UtcDateTime,
            read.Artifacts.Select(a => (a.Key, a.FileName, a.ContentType)),
            now);

        if (previousBodyKey is not null)
            orphanedKeys.Add(previousBodyKey);

        var reingested = await SyncItemsAsync(source, message, existing, read, now, orphanedKeys, cancellationToken);

        var newBodyKey = await StoreBodyAsync(source, message, read, now, cancellationToken);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        // Passo 4: só depois do commit, e só o que ficou órfão. Falhar aqui não desfaz nada —
        // um blob a mais no balde é preferível a uma linha apontando para o vazio.
        await RemoveOrphansAsync(tenantId, orphanedKeys.Where(k => k != newBodyKey), cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "E-mail recapturado: {Reingested} artefatos reingeridos, {Cancelled} boletos cancelados, {Denied} já negados.",
                reingested, plan.BillsToCancel.Count, plan.PreviouslyDeniedBills.Count);
        }

        return new RecaptureMessageResponse(
            message.Id.Value,
            reingested,
            plan.BillsToCancel.Count,
            [.. plan.PreviouslyDeniedBills.Select(b => b.Value)]);
    }

    private async Task<MailboxMessage> FetchFromProviderAsync(
        CaptureSource source,
        CapturedMessage message,
        Guid capturedMessageId,
        CancellationToken cancellationToken)
    {
        // Reencontra pelo identificador do cabeçalho, que não muda quando a mensagem é movida.
        var relocated = await mailboxReader.RelocateArtifactAsync(
            source.Address, source.Credential!, message.InternetMessageId!, fileName: null, cancellationToken)
            ?? throw CapturedMessageErrors.RecaptureSourceMessageNotFound(capturedMessageId);

        // O que o provedor devolve agora pode diferir do que devolveu antes — anexo apagado, tipo
        // diferente. Reingerir com o metadado velho descreveria um e-mail que não existe.
        return await mailboxReader.ReadSingleMessageAsync(
            source.Address, source.Credential!, relocated.ExternalMessageId, cancellationToken)
            ?? throw CapturedMessageErrors.RecaptureSourceMessageNotFound(capturedMessageId);
    }

    private async Task<IReadOnlyCollection<(CaptureItem Item, Bill? Bill)>> LinkBillsAsync(
        TenantId tenantId,
        IReadOnlyList<CaptureItem> existing,
        CancellationToken cancellationToken)
    {
        var linked = new List<(CaptureItem, Bill?)>(existing.Count);

        foreach (var item in existing)
        {
            var bill = item.BillId is { } billId
                ? await bills.GetAsync(tenantId, billId, cancellationToken)
                : null;

            linked.Add((item, bill));
        }

        return linked;
    }

    /// <summary>
    /// Item por anexo, casado pela chave: o que continua existindo é reescrito, o que sumiu sai,
    /// o que é novo entra. Devolve quantos anexos vão passar pela triagem.
    /// </summary>
    private async Task<int> SyncItemsAsync(
        CaptureSource source,
        CapturedMessage message,
        IReadOnlyList<CaptureItem> existing,
        MailboxMessage read,
        DateTime occurredAt,
        List<string> orphanedKeys,
        CancellationToken cancellationToken)
    {
        var byKey = existing.ToDictionary(i => i.ArtifactKey, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var artifact in read.Artifacts)
        {
            seen.Add(artifact.Key);

            if (byKey.TryGetValue(artifact.Key, out var item))
            {
                // O blob de um item que virou boleto é a evidência do boleto — mesmo cancelado,
                // a Bill continua apontando para ele. Só o que nunca virou boleto é órfão.
                var hadBill = item.BillId is not null;
                var previousKey = item.Recapture(
                    read.MessageId, artifact.ContentType, artifact.FileName,
                    read.Sender, read.Subject, read.ReceivedAt.UtcDateTime, occurredAt);

                if (previousKey is not null && !hadBill)
                    orphanedKeys.Add(previousKey);

                continue;
            }

            var ingested = CaptureItem.Ingest(
                source.TenantId,
                source.Id,
                read.MessageId,
                artifact.Key,
                read.Sender,
                read.Subject,
                read.ReceivedAt.UtcDateTime,
                occurredAt,
                artifact.ContentType,
                artifact.FileName,
                message.InternetMessageId);

            await items.AddAsync(ingested, cancellationToken);
        }

        foreach (var item in existing)
        {
            if (seen.Contains(item.ArtifactKey))
                continue;

            if (item.HasStoredArtifact && item.BillId is null)
                orphanedKeys.Add(item.StorageKey!);

            items.Remove(item);
        }

        return read.Artifacts.Count;
    }

    private async Task<string?> StoreBodyAsync(
        CaptureSource source,
        CapturedMessage message,
        MailboxMessage read,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        // O corpo acompanha o registro — mesma retenção da sincronização, pela mesma chamada
        // dedicada; a falha deixa o registro sem corpo e a tela cai no plano B.
        var body = await mailboxReader.DownloadArtifactAsync(
            source.Address, source.Credential!, read.MessageId, IMailboxReader.BODY_ARTIFACT_KEY, cancellationToken);

        if (body is not { IsEmpty: false })
            return null;

        var contentType = read.Artifacts
            .FirstOrDefault(a => string.Equals(a.Key, IMailboxReader.BODY_ARTIFACT_KEY, StringComparison.Ordinal))
            ?.ContentType ?? "text/html";

        var key = await storage.StoreAsync(
            source.TenantId, $"message-body-{message.Id.Value:N}", contentType, body.Value, cancellationToken);

        message.RecordBody(key, contentType, occurredAt);
        return key;
    }

    private async Task RemoveOrphansAsync(
        TenantId tenantId,
        IEnumerable<string> keys,
        CancellationToken cancellationToken)
    {
        foreach (var key in keys.Distinct(StringComparer.Ordinal))
        {
            try
            {
                await storage.RemoveAsync(tenantId, key, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Blob órfão é o custo aceitável; a recaptura já está commitada. A chave não vai
                // para o log — é ponteiro de infraestrutura, e o motivo basta para investigar.
                logger.LogWarning(ex, "Não foi possível apagar um artefato órfão depois da recaptura.");
            }
        }
    }
}

public sealed class RecaptureMessageIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RecaptureMessageIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RecaptureMessageCommand, RecaptureMessageResponse>(
        mediator, requestManager, logger)
{
    protected override RecaptureMessageResponse CreateResultForDuplicateRequest() => new(Guid.Empty, 0, 0, []);
}
