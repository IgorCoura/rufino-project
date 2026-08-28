namespace BillPayment.Application.CapturedMessages.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CapturedMessages;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Apaga tudo que a captura produziu para um e-mail e o reingere como se tivesse acabado de
/// chegar.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Diferente de reprocessar um item.</strong> Reprocessar devolve à cascata um item que
/// ainda existe, com os mesmos ids; a recaptura serve ao e-mail cujo item foi <em>descartado</em>
/// — e que portanto não tem o que reprocessar — e ao que ficou preso num endereço de
/// armazenamento morto.
/// </para>
/// <para>
/// <strong>Busca primeiro, apaga depois.</strong> Se a mensagem não existir mais na caixa, a
/// operação falha e nada é perdido. A ordem inversa trocaria um histórico incompleto por
/// histórico nenhum.
/// </para>
/// <para>
/// <c>IMultiAggregateCommand</c> porque toca o registro, os itens e a fonte na mesma transação:
/// apagar o item numa e reingerir noutra deixaria o e-mail sem item e sem registro no meio do
/// caminho, que é exatamente o estado que esta operação existe para não produzir.
/// </para>
/// </remarks>
public sealed record RecaptureMessageCommand(Guid TenantId, Guid CapturedMessageId)
    : IRequest<RecaptureMessageResponse>, IMultiAggregateCommand;

/// <param name="ItemsRemoved">Quantos itens de captura foram apagados para dar lugar aos novos.</param>
/// <param name="ArtifactsIngested">Quantos artefatos entraram de novo.</param>
public sealed record RecaptureMessageResponse(Guid Id, int ItemsRemoved, int ArtifactsIngested);

public sealed class RecaptureMessageCommandHandler(
    ICapturedMessageRepository capturedMessages,
    ICaptureItemRepository items,
    ICaptureSourceRepository sources,
    IMailboxReader mailboxReader,
    IAttachmentStorage storage,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<RecaptureMessageCommandHandler> logger)
    : IRequestHandler<RecaptureMessageCommand, RecaptureMessageResponse>
{
    public async Task<RecaptureMessageResponse> Handle(
        RecaptureMessageCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var id = CapturedMessageId.From(request.CapturedMessageId);

        var message = await capturedMessages.GetAsync(tenantId, id, cancellationToken)
            ?? throw CapturedMessageErrors.NotFound(request.CapturedMessageId);

        message.EnsureCanBeRecaptured();

        var source = await sources.GetAsync(tenantId, message.SourceId, cancellationToken)
            ?? throw CaptureSourceErrors.NotFound(message.SourceId.Value);

        // Passo 1: achar a mensagem onde ela estiver hoje. Falhar aqui não custa nada — nada foi
        // apagado ainda.
        var relocated = await mailboxReader.RelocateArtifactAsync(
            source.Address,
            source.Credential!,
            message.InternetMessageId!,
            fileName: null,
            cancellationToken)
            ?? throw CapturedMessageErrors.CannotRecaptureWithoutInternetMessageId(request.CapturedMessageId);

        var now = clock.GetUtcNow().UtcDateTime;

        // Passo 2: só agora apaga o que existia — o corpo guardado sai junto do registro velho,
        // porque o novo grava o dele com outra chave.
        var removed = await PurgeExistingItemsAsync(message, tenantId, cancellationToken);
        if (message.HasStoredBody)
            await storage.RemoveAsync(tenantId, message.BodyStorageKey!, cancellationToken);
        capturedMessages.Remove(message);

        // Passo 3: reingere pelo caminho normal. O cursor da pasta não é tocado — o que traz esta
        // mensagem de volta é o item nascer de novo, não a caixa ser relida inteira.
        var reingested = await ReingestAsync(source, message, relocated, now, cancellationToken);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "E-mail recapturado: {Removed} itens apagados, {Ingested} artefatos reingeridos.",
                removed, reingested);
        }

        return new RecaptureMessageResponse(request.CapturedMessageId, removed, reingested);
    }

    /// <summary>
    /// Apaga os itens e os arquivos que este e-mail produziu.
    /// </summary>
    /// <remarks>
    /// <strong>O boleto não é apagado.</strong> A <c>Bill</c> não referencia o item, então
    /// removê-lo não a deixa órfã — e, se o mesmo instrumento reentrar, a chave única global faz
    /// o item novo apontar para o boleto que já existe, em vez de criar outro.
    /// </remarks>
    private async Task<int> PurgeExistingItemsAsync(
        CapturedMessage message,
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        var removed = 0;

        foreach (var artifact in message.Artifacts)
        {
            if (artifact.CaptureItemId is not { } itemId)
                continue;

            var item = await items.GetAsync(tenantId, itemId, cancellationToken);
            if (item is null)
                continue;

            if (item.HasStoredArtifact)
                await storage.RemoveAsync(tenantId, item.StorageKey!, cancellationToken);

            items.Remove(item);
            removed++;
        }

        return removed;
    }

    /// <summary>Relê a mensagem no provedor e recria registro e itens com os ids de hoje.</summary>
    private async Task<int> ReingestAsync(
        CaptureSource source,
        CapturedMessage previous,
        RelocatedArtifact relocated,
        DateTime occurredAt,
        CancellationToken cancellationToken)
    {
        // O que o provedor devolve agora pode diferir do que ele devolveu antes — anexo apagado,
        // tipo diferente. Reingerir com o metadado velho descreveria um e-mail que não existe.
        var read = await mailboxReader.ReadSingleMessageAsync(
            source.Address, source.Credential!, relocated.ExternalMessageId, cancellationToken);

        if (read is null)
            throw CapturedMessageErrors.NotFound(previous.Id.Value);

        var captured = CapturedMessage.Register(
            source.TenantId,
            source.Id,
            read.MessageId,
            read.Sender,
            read.Subject,
            read.ReceivedAt.UtcDateTime,
            occurredAt,
            read.Artifacts.Select(a => (a.Key, a.FileName, a.ContentType)),
            read.InternetMessageId);

        // O corpo do e-mail acompanha o registro novo — mesma retenção da sincronização, pela
        // mesma chamada dedicada; a falha deixa o registro sem corpo e a tela cai no plano B.
        var body = await mailboxReader.DownloadArtifactAsync(
            source.Address, source.Credential!, read.MessageId, IMailboxReader.BODY_ARTIFACT_KEY, cancellationToken);

        if (body is { IsEmpty: false })
        {
            var contentType = read.Artifacts
                .FirstOrDefault(a => string.Equals(a.Key, IMailboxReader.BODY_ARTIFACT_KEY, StringComparison.Ordinal))
                ?.ContentType ?? "text/html";

            var bodyKey = await storage.StoreAsync(
                source.TenantId, $"message-body-{captured.Id.Value:N}", contentType, body.Value, cancellationToken);

            captured.RecordBody(bodyKey, contentType, occurredAt);
        }

        await capturedMessages.AddAsync(captured, cancellationToken);

        var ingested = 0;

        foreach (var artifact in read.Artifacts)
        {
            var item = CaptureItem.Ingest(
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
                read.InternetMessageId);

            await items.AddAsync(item, cancellationToken);
            ingested++;
        }

        return ingested;
    }
}

public sealed class RecaptureMessageIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RecaptureMessageIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RecaptureMessageCommand, RecaptureMessageResponse>(
        mediator, requestManager, logger)
{
    protected override RecaptureMessageResponse CreateResultForDuplicateRequest() => new(Guid.Empty, 0, 0);
}
