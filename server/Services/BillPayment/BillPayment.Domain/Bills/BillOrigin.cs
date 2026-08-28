namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SeedWork;

/// <summary>
/// De onde o documento veio, com evidência suficiente para reconstruir o caminho depois.
/// </summary>
/// <remarks>
/// Alimenta o check de origem confiável e a auditoria: quando um pagamento é contestado, a
/// primeira pergunta é "de onde isso saiu?", e a resposta precisa ter sido gravada no momento
/// da captura — a caixa de e-mail pode ter sido esvaziada desde então.
/// </remarks>
public sealed class BillOrigin : ValueObject
{
    public const int SENDER_ADDRESS_MAX_LENGTH = 320;
    public const int EXTERNAL_MESSAGE_ID_MAX_LENGTH = 500;
    public const int STORAGE_KEY_MAX_LENGTH = 500;
    /// <summary>
    /// O mesmo limite do <c>CaptureItem.ContentHash</c>, e por isso 100 e não 64.
    /// </summary>
    /// <remarks>
    /// O hash viaja <strong>com o nome do algoritmo</strong> (<c>sha256:</c> + 64 hexadecimais =
    /// 71 caracteres), para que trocar de algoritmo depois não torne ilegível o que já foi
    /// gravado. Dimensionar este campo para o hash cru fazia a promoção de um item capturado
    /// estourar na criação da origem — o valor vem de lá, e os dois lados precisam descrever o
    /// mesmo dado com o mesmo tamanho.
    /// </remarks>
    public const int CONTENT_HASH_MAX_LENGTH = 100;

    public BillSourceKind SourceKind { get; private set; } = default!;

    /// <summary>A <c>CaptureSource</c> que trouxe o documento. Nulo em upload manual.</summary>
    public Guid? SourceId { get; private set; }

    /// <summary>Remetente do e-mail, normalizado. É contra ele que a confiança de origem é resolvida.</summary>
    public string? SenderAddress { get; private set; }

    /// <summary>Id da mensagem no provedor, para reencontrar o original.</summary>
    public string? ExternalMessageId { get; private set; }

    public DateTime ReceivedAt { get; private set; }

    /// <summary>SHA-256 do arquivo, para detectar reenvio do mesmo documento.</summary>
    public string? ContentHash { get; private set; }

    /// <summary>Onde o arquivo original foi guardado.</summary>
    public string? StorageKey { get; private set; }

    private BillOrigin() { }

    public static BillOrigin Create(
        BillSourceKind sourceKind,
        DateTime receivedAt,
        Guid? sourceId = null,
        string? senderAddress = null,
        string? externalMessageId = null,
        string? contentHash = null,
        string? storageKey = null)
    {
        if (sourceKind is null)
            throw BillErrors.OriginSourceKindRequired();

        if (sourceKind.RequiresCaptureSource && (sourceId is null || sourceId == Guid.Empty))
            throw BillErrors.OriginSourceIdRequired(sourceKind.Name);

        var sender = Normalize(senderAddress, "remetente", SENDER_ADDRESS_MAX_LENGTH);
        var messageId = Normalize(externalMessageId, "mensagem de origem", EXTERNAL_MESSAGE_ID_MAX_LENGTH);
        var storage = Normalize(storageKey, "arquivo", STORAGE_KEY_MAX_LENGTH);
        var hash = Normalize(contentHash, "hash do conteúdo", CONTENT_HASH_MAX_LENGTH);

        // Sem nenhum identificador, a origem não reconstrói caminho nenhum e a evidência
        // seria só o carimbo de data — que não distingue um documento de outro. Vale para o que
        // entrou sozinho; na importação manual a identidade é o instrumento digitado, e exigir
        // rastro externo ali recusava toda importação feita pela tela (ver RequiresOriginIdentifier).
        if (sourceKind.RequiresOriginIdentifier
            && sourceId is null && sender is null && messageId is null && storage is null && hash is null)
            throw BillErrors.OriginWithoutAnyIdentifier();

        return new BillOrigin
        {
            SourceKind = sourceKind,
            SourceId = sourceId == Guid.Empty ? null : sourceId,
            SenderAddress = sender?.ToLowerInvariant(),
            ExternalMessageId = messageId,
            ReceivedAt = receivedAt,
            ContentHash = hash?.ToLowerInvariant(),
            StorageKey = storage,
        };
    }

    private static string? Normalize(string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? throw BillErrors.OriginFieldTooLong(field, maxLength) : trimmed;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SourceKind;
        yield return SourceId;
        yield return SenderAddress;
        yield return ExternalMessageId;
        yield return ReceivedAt;
        yield return ContentHash;
        yield return StorageKey;
    }
}
