namespace BillPayment.Domain.CaptureItems;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O registro bruto de um artefato ingerido — um anexo, um link ou o corpo de uma mensagem —
/// incluindo os que nunca viraram boleto.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Um item por artefato, não por mensagem.</strong> Um e-mail com três boletos anexos
/// produz três itens, porque cada um tem seu próprio destino: um pode virar <c>Bill</c>, outro
/// ser de outro pagador e o terceiro nem ser boleto. Com um item por mensagem, o estado seria
/// misto e a projeção de visibilidade do ADR-008 teria de existir por anexo — dentro de um
/// agregado cujo status diz outra coisa. A idempotência da ingestão passa a ser
/// <c>(TenantId, SourceId, ExternalMessageId, ArtifactKey)</c>.
/// </para>
/// <para>
/// A quarentena tem <strong>dois níveis de visibilidade</strong>, e quem os decide é o
/// <see cref="CaptureItemStatus"/>, não a tela: ver <c>ExposesFinancialDetail</c>.
/// </para>
/// </remarks>
public sealed class CaptureItem : AggregateRoot<CaptureItemId>
{
    public const int EXTERNAL_MESSAGE_ID_MAX_LENGTH = 512;
    public const int ARTIFACT_KEY_MAX_LENGTH = 512;
    public const int SENDER_MAX_LENGTH = 320;
    public const int SUBJECT_MAX_LENGTH = 500;
    public const int CONTENT_TYPE_MAX_LENGTH = 150;
    public const int FILE_NAME_MAX_LENGTH = 255;
    public const int CONTENT_HASH_MAX_LENGTH = 100;
    public const int STORAGE_KEY_MAX_LENGTH = 512;
    public const int SOURCE_URL_MAX_LENGTH = 2000;
    public const int REASON_MAX_LENGTH = 200;
    public const int UNLOCKED_BY_MAX_LENGTH = 100;

    public TenantId TenantId { get; private set; }
    public CaptureSourceId SourceId { get; private set; }

    /// <summary>Id da mensagem no provedor. Estável entre sincronizações — é o que dá idempotência.</summary>
    public string ExternalMessageId { get; private set; } = string.Empty;

    /// <summary>
    /// Qual artefato daquela mensagem é este: nome do anexo, hash da URL, ou a marca do corpo.
    /// Distingue os irmãos que compartilham o mesmo <see cref="ExternalMessageId"/>.
    /// </summary>
    public string ArtifactKey { get; private set; } = string.Empty;

    /// <summary>
    /// Tipo de mídia declarado pelo provedor na ingestão.
    /// </summary>
    /// <remarks>
    /// <strong>Guardado porque o <see cref="ArtifactKey"/> não é nome de arquivo.</strong> No
    /// Microsoft Graph ele é um identificador opaco, sem extensão nenhuma — deduzir o tipo dali
    /// faz todo anexo parecer PDF. Medido em 2026-08-11: o extrator de visão recebia imagem
    /// rotulada como <c>application/pdf</c> e o provedor recusava, então os anexos que não eram
    /// PDF continuavam inalcançáveis mesmo depois de a visão existir.
    /// </remarks>
    public string? ContentType { get; private set; }

    /// <summary>Nome do arquivo, quando o provedor informa. Diagnóstico e triagem — nunca chave.</summary>
    public string? FileName { get; private set; }

    public string Sender { get; private set; } = string.Empty;
    public string? Subject { get; private set; }
    public DateTime ReceivedAt { get; private set; }

    /// <summary>SHA-256 do artefato, para detectar o mesmo documento reenviado noutra mensagem.</summary>
    public string? ContentHash { get; private set; }

    /// <summary>Onde os bytes originais estão, cifrados. Nulo enquanto o download não aconteceu.</summary>
    public string? StorageKey { get; private set; }

    public CaptureItemStatus Status { get; private set; } = default!;

    /// <summary>Por qual degrau da escada este item foi atribuído. Nulo antes do roteamento.</summary>
    public RoutingConfidence? Routing { get; private set; }

    /// <summary>URL de origem quando o artefato veio de link — evidência e reprocesso.</summary>
    public string? SourceUrl { get; private set; }

    /// <summary>
    /// <strong>Qual campo</strong> do <c>PayerProfile</c> derivou a senha do PDF — jamais a
    /// senha. É o que permite auditar a decisão sem transformar o banco num depósito de segredos.
    /// </summary>
    public string? UnlockedBy { get; private set; }

    public ExtractionMethod? Extraction { get; private set; }

    /// <summary>Motivo legível do desfecho de quarentena, em código estável.</summary>
    public string? Reason { get; private set; }

    public BillId? BillId { get; private set; }

    /// <summary>O item original, quando este foi descartado por duplicidade de conteúdo.</summary>
    public CaptureItemId? DiscardedOf { get; private set; }

    public UserId? ClaimedBy { get; private set; }
    public DateTime? ClaimedAt { get; private set; }

    private CaptureItem() { }

    private CaptureItem(CaptureItemId id) : base(id) { }

    /// <summary>Registra o artefato como recebido, antes de qualquer tentativa de leitura.</summary>
    public static CaptureItem Ingest(
        TenantId tenantId,
        CaptureSourceId sourceId,
        string externalMessageId,
        string artifactKey,
        string sender,
        string? subject,
        DateTime receivedAt,
        DateTime occurredAt,
        string? contentType = null,
        string? fileName = null)
    {
        if (sourceId.Equals(CaptureSourceId.Empty))
            throw CaptureItemErrors.SourceRequired();

        var item = new CaptureItem(CaptureItemId.New())
        {
            TenantId = tenantId,
            SourceId = sourceId,
            ReceivedAt = receivedAt,
            Status = CaptureItemStatus.Received,
        };

        item.SetExternalMessageId(externalMessageId);
        item.SetArtifactKey(artifactKey);
        item.SetSender(sender);
        item.SetSubject(subject);
        item.SetContentType(contentType);
        item.SetFileName(fileName);

        item.CreatedAt = occurredAt;
        item.UpdatedAt = occurredAt;
        return item;
    }

    /// <summary>
    /// Guarda onde os bytes ficaram e o hash do conteúdo. Não muda o status — armazenar é
    /// pré-requisito do processamento, não um desfecho dele.
    /// </summary>
    public void StoreArtifact(string contentHash, string storageKey, DateTime occurredAt)
    {
        var hash = Require(contentHash, nameof(ContentHash), CONTENT_HASH_MAX_LENGTH);
        var key = Require(storageKey, nameof(StorageKey), STORAGE_KEY_MAX_LENGTH);

        ContentHash = hash;
        StorageKey = key;
        UpdatedAt = occurredAt;
    }

    /// <summary>O artefato precisa ser baixado de uma URL antes de poder ser lido.</summary>
    public void MarkLinkPending(string sourceUrl, DateTime occurredAt)
    {
        var url = sourceUrl?.Trim();
        if (string.IsNullOrEmpty(url))
            throw CaptureItemErrors.SourceUrlRequired();
        if (url.Length > SOURCE_URL_MAX_LENGTH)
            throw CaptureItemErrors.TextTooLong(nameof(SourceUrl), SOURCE_URL_MAX_LENGTH);

        Transition(CaptureItemStatus.LinkPending, occurredAt);
        SourceUrl = url;
    }

    /// <summary>
    /// Registra de qual endereço o documento foi trazido, sem mexer no status.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Não é transição, é procedência.</strong> A resolução do link acontece dentro do
    /// mesmo processamento que já vai decidir o destino do item — passar por <c>LinkPending</c> só
    /// para voltar no instante seguinte inventaria um estado intermediário que ninguém observa e
    /// que a máquina de estados teria de admitir em todas as direções.
    /// </para>
    /// <para>
    /// <strong>A URL é tão sigilosa quanto o documento.</strong> Medido em 2026-08-11: os endereços
    /// de boleto respondem <c>200</c> sem autenticação nenhuma — quem tem o link tem o boleto. Ele
    /// sai por API só sob o mesmo portão do ADR-008 que já cobre o <see cref="StorageKey"/>.
    /// </para>
    /// </remarks>
    public void RecordResolvedLink(string sourceUrl, DateTime occurredAt)
    {
        var url = sourceUrl?.Trim();
        if (string.IsNullOrEmpty(url))
            throw CaptureItemErrors.SourceUrlRequired();
        if (url.Length > SOURCE_URL_MAX_LENGTH)
            throw CaptureItemErrors.TextTooLong(nameof(SourceUrl), SOURCE_URL_MAX_LENGTH);

        SourceUrl = url;
        UpdatedAt = occurredAt;
    }

    /// <summary>A escada de resolução esgotou sem trazer o documento.</summary>
    public void MarkLinkFailed(string reason, DateTime occurredAt)
    {
        Transition(CaptureItemStatus.LinkFailed, occurredAt);
        Reason = RequireReason(reason);
    }

    /// <summary>PDF cifrado que nenhum candidato de senha abriu.</summary>
    public void MarkLocked(DateTime occurredAt)
    {
        EnsureArtifactStored();
        Transition(CaptureItemStatus.Locked, occurredAt);
        Reason = "pdf_locked";
    }

    /// <summary>
    /// Um instrumento de pagamento válido foi extraído. <paramref name="unlockedBy"/> registra
    /// qual campo do perfil abriu o PDF, quando foi o caso.
    /// </summary>
    public void MarkParsed(ExtractionMethod method, string? unlockedBy, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(method);
        EnsureArtifactStored();

        var derivedFrom = unlockedBy?.Trim();
        if (derivedFrom is { Length: > UNLOCKED_BY_MAX_LENGTH })
            throw CaptureItemErrors.TextTooLong(nameof(UnlockedBy), UNLOCKED_BY_MAX_LENGTH);

        Transition(CaptureItemStatus.Parsed, occurredAt);
        Extraction = method;
        UnlockedBy = string.IsNullOrEmpty(derivedFrom) ? null : derivedFrom;
        Reason = null;
    }

    /// <summary>Nenhum boleto válido no artefato.</summary>
    public void MarkUnrecognized(string reason, DateTime occurredAt)
    {
        Transition(CaptureItemStatus.Unrecognized, occurredAt);
        Reason = RequireReason(reason);
    }

    /// <summary>
    /// Devolve o artefato à fila para ser avaliado de novo pela cascata de hoje.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>O desfecho de um artefato é do dia em que ele passou, não para sempre.</strong> A
    /// cascata muda — degrau novo, prompt novo, modelo novo — e o cadastro também: sem
    /// <c>PayerProfile</c> não há senha derivada, e sem <c>Payee</c> nem <c>TrustedOrigin</c> o
    /// que o parser erra é descartado. Um item julgado antes disso ficaria congelado num veredito
    /// que a versão atual do sistema não daria.
    /// </para>
    /// <para>
    /// <strong>Volta para <c>Received</c> em vez de reprocessar aqui</strong>: assim ele atravessa
    /// exatamente o mesmo caminho do primeiro processamento, pelo mesmo worker, com a mesma
    /// transação e a mesma retenção por desfecho. Um segundo caminho de processamento seria um
    /// segundo lugar para as regras envelhecerem.
    /// </para>
    /// <para>
    /// <strong>Nada é apagado.</strong> O artefato continua no armazenamento e a chave continua
    /// no item — é justamente o que permite reavaliar sem baixar de novo do provedor.
    /// </para>
    /// </remarks>
    public void Reopen(DateTime occurredAt)
    {
        Transition(CaptureItemStatus.Received, occurredAt);
        Reason = null;
        Extraction = null;
        UnlockedBy = null;

        // A procedência também é do dia em que o item passou: a escada de link vai ser percorrida
        // de novo, com as receitas de hoje, e pode trazer o documento de outro endereço — ou de
        // nenhum. Manter o anterior descreveria uma busca que não foi a que aconteceu.
        SourceUrl = null;
    }

    /// <summary>Roteou para este tenant e virou boleto.</summary>
    public void Promote(BillId billId, RoutingConfidence confidence, DateTime occurredAt)
    {
        if (billId.Equals(Bills.BillId.Empty))
            throw CaptureItemErrors.BillRequired();
        if (confidence is null)
            throw CaptureItemErrors.RoutingConfidenceRequired();

        Transition(CaptureItemStatus.Promoted, occurredAt);
        BillId = billId;
        Routing = confidence;
        Reason = null;
    }

    /// <summary>O pagador foi identificado e não é deste tenant.</summary>
    public void MarkForeign(string reason, DateTime occurredAt)
    {
        Transition(CaptureItemStatus.ForeignPayer, occurredAt);
        Reason = RequireReason(reason);
    }

    /// <summary>Nada resolveu de quem é. Vai para a fila de reivindicação do dono da fonte.</summary>
    public void MarkUnrouted(string reason, DateTime occurredAt)
    {
        Transition(CaptureItemStatus.Unrouted, occurredAt);
        Reason = RequireReason(reason);
    }

    /// <summary>
    /// Uma pessoa assumiu que o item é desta conta, e o item vira boleto por essa decisão.
    /// </summary>
    /// <remarks>
    /// Recusado quando a escada já concluiu que o pagador é outro (<c>BLP.CPI04</c>): a
    /// reivindicação é o degrau mais fraco de todos, e deixá-la sobrepor a única evidência
    /// <em>constatada</em> de propriedade inverteria a ordem de confiança do doc 07.
    /// </remarks>
    public void Claim(UserId claimedBy, BillId billId, DateTime occurredAt)
    {
        if (claimedBy.Equals(UserId.Empty))
            throw CaptureItemErrors.ClaimedByRequired();
        if (Status == CaptureItemStatus.ForeignPayer)
            throw CaptureItemErrors.ClaimContradictsExtractedPayer(Id.Value);

        Promote(billId, RoutingConfidence.Claimed, occurredAt);
        ClaimedBy = claimedBy;
        ClaimedAt = occurredAt;
    }

    /// <summary>Duplicata de um artefato já processado, com o ponteiro para o original.</summary>
    public void Discard(CaptureItemId originalItemId, DateTime occurredAt)
    {
        Transition(CaptureItemStatus.Discarded, occurredAt);
        DiscardedOf = originalItemId.Equals(CaptureItemId.Empty) ? null : originalItemId;
        Reason = "duplicate_content";
    }

    private void Transition(CaptureItemStatus target, DateTime occurredAt)
    {
        if (!Status.CanTransitionTo(target))
            throw CaptureItemErrors.InvalidTransition(Status.Name, target.Name);

        Status = target;
        UpdatedAt = occurredAt;
    }

    private void EnsureArtifactStored()
    {
        if (string.IsNullOrEmpty(StorageKey))
            throw CaptureItemErrors.ArtifactRequired();
    }

    private void SetExternalMessageId(string value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw CaptureItemErrors.ExternalMessageIdRequired();
        if (trimmed.Length > EXTERNAL_MESSAGE_ID_MAX_LENGTH)
            throw CaptureItemErrors.TextTooLong(nameof(ExternalMessageId), EXTERNAL_MESSAGE_ID_MAX_LENGTH);

        ExternalMessageId = trimmed;
    }

    private void SetArtifactKey(string value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw CaptureItemErrors.ArtifactKeyRequired();
        if (trimmed.Length > ARTIFACT_KEY_MAX_LENGTH)
            throw CaptureItemErrors.TextTooLong(nameof(ArtifactKey), ARTIFACT_KEY_MAX_LENGTH);

        ArtifactKey = trimmed;
    }

    /// <summary>
    /// Guarda o tipo declarado, aparado — nunca deduzido.
    /// </summary>
    /// <remarks>
    /// Truncar em vez de recusar: o tipo vem do provedor e um valor esquisito não pode impedir a
    /// ingestão de um boleto. Quem decide se o extrator sabe abrir aquilo é <c>DocumentPayload</c>.
    /// </remarks>
    private void SetContentType(string? value)
    {
        var trimmed = value?.Trim();
        ContentType = string.IsNullOrEmpty(trimmed)
            ? null
            : trimmed[..Math.Min(trimmed.Length, CONTENT_TYPE_MAX_LENGTH)];
    }

    private void SetFileName(string? value)
    {
        var trimmed = value?.Trim();
        FileName = string.IsNullOrEmpty(trimmed)
            ? null
            : trimmed[..Math.Min(trimmed.Length, FILE_NAME_MAX_LENGTH)];
    }

    private void SetSender(string value)
    {
        // Remetente vem de fora e não decide nada sozinho — quem decide confiança é o
        // TrustedOrigin. Normalizar aqui é o que faz a resolução casar com o que foi cadastrado.
        var normalized = EmailSyntax.Normalize(value);
        if (normalized.Length > SENDER_MAX_LENGTH)
            throw CaptureItemErrors.TextTooLong(nameof(Sender), SENDER_MAX_LENGTH);

        Sender = normalized;
    }

    private void SetSubject(string? value)
    {
        var trimmed = value?.Trim();
        if (trimmed is { Length: > SUBJECT_MAX_LENGTH })
            trimmed = trimmed[..SUBJECT_MAX_LENGTH];

        Subject = string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string RequireReason(string reason)
    {
        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw CaptureItemErrors.ReasonRequired();
        if (trimmed.Length > REASON_MAX_LENGTH)
            throw CaptureItemErrors.TextTooLong(nameof(Reason), REASON_MAX_LENGTH);

        return trimmed;
    }

    private static string Require(string value, string field, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw CaptureItemErrors.ArtifactRequired();
        if (trimmed.Length > maxLength)
            throw CaptureItemErrors.TextTooLong(field, maxLength);

        return trimmed;
    }
}
