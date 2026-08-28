namespace BillPayment.Application.Queries.CapturedMessages;

/// <summary>
/// Um e-mail lido, como a tela de histórico o mostra.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Metadado, nunca conteúdo.</strong> Não sai chave de armazenamento, nem link, nem
/// bytes: o que este DTO existe para responder é "o que aconteceu com o e-mail que eu mandei",
/// e nada além disso justifica trafegar.
/// </para>
/// <para>
/// O <c>internetMessageId</c> também não sai — ele é a chave que a recaptura usa do lado do
/// servidor, e entregá-lo à tela seria repetir o erro da chave de armazenamento.
/// </para>
/// </remarks>
/// <param name="Outcome">O desfecho dominante — o que a linha mostra sem expandir.</param>
/// <param name="ArtifactCount">Quantos anexos o e-mail trazia.</param>
/// <param name="CanRecapture">
/// Se dá para reprocessar do zero. Falso em e-mail capturado antes de o sistema guardar o
/// identificador permanente da mensagem.
/// </param>
public sealed record CapturedMessageDto(
    Guid Id,
    Guid SourceId,
    string Sender,
    string? Subject,
    DateTime ReceivedAt,
    DateTime FirstSeenAt,
    DateTime? ProcessedAt,
    string Outcome,
    int ArtifactCount,
    bool CanRecapture,
    IReadOnlyList<MessageArtifactDto> Artifacts);

/// <param name="CaptureItemId">
/// Para onde a tela navega. Nulo quando o artefato foi descartado — o item não existe mais, e
/// oferecer o link devolveria 404.
/// </param>
public sealed record MessageArtifactDto(
    string? FileName,
    string? ContentType,
    string Outcome,
    string? Reason,
    Guid? CaptureItemId,
    Guid? BillId,
    DateTime? DecidedAt);

public sealed record CapturedMessagePage(IReadOnlyList<CapturedMessageDto> Items, string? NextCursor);

/// <summary>O cabeçalho da tela: quando a caixa foi lida pela última vez.</summary>
/// <param name="LastSyncAt">Nulo quando nenhuma fonte sincronizou ainda.</param>
public sealed record CaptureSyncStatusDto(DateTime? LastSyncAt, int SourceCount);

/// <summary>
/// O e-mail como a tela o mostra: cabeçalho e corpo. <strong>Sai por API só sob o portão do
/// documento original (ADR-008)</strong> — o corpo pode carregar instrumento de pagamento.
/// </summary>
public sealed record CapturedMessageBodyDto(
    Guid Id,
    string Sender,
    string? Subject,
    DateTime ReceivedAt,
    string ContentType,
    string Content);
