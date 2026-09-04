namespace BillPayment.Domain.CapturedMessages;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O livro-caixa da captura: um registro por e-mail lido, <strong>inclusive os que não eram
/// boleto</strong>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Por que existe separado do <c>CaptureItem</c>.</strong> O item é a fila de trabalho e
/// vive por artefato; quando a triagem descarta, a linha é apagada e não sobra nada. Isso é
/// deliberado — jogar e-mail irrelevante na quarentena a tornaria inutilizável, e a medição de
/// 2026-08-11 achou 250 de 404 anexos sem sinal nenhum de cobrança. O custo era que a pessoa que
/// mandou um e-mail e não viu nada acontecer ficava sem resposta. Este agregado é a resposta.
/// </para>
/// <para>
/// <strong>Metadado, nunca bytes.</strong> A retenção por desfecho não muda: o arquivo do que foi
/// descartado continua não sendo guardado. Aqui ficam remetente, assunto, datas e o que se
/// decidiu — o suficiente para explicar, insuficiente para reconstruir o documento.
/// </para>
/// <para>
/// Um registro por <c>(TenantId, SourceId, ExternalMessageId)</c>, e um
/// <see cref="MessageArtifact"/> por anexo. Não emite Domain Event: ninguém reage a ele.
/// </para>
/// </remarks>
public sealed class CapturedMessage : AggregateRoot<CapturedMessageId>
{
    public const int EXTERNAL_MESSAGE_ID_MAX_LENGTH = 512;
    public const int INTERNET_MESSAGE_ID_MAX_LENGTH = 512;
    public const int SENDER_MAX_LENGTH = 320;
    public const int SUBJECT_MAX_LENGTH = 500;
    public const int BODY_STORAGE_KEY_MAX_LENGTH = 512;
    public const int BODY_CONTENT_TYPE_MAX_LENGTH = 150;

    public TenantId TenantId { get; private set; }
    public CaptureSourceId SourceId { get; private set; }

    /// <summary>Id da mensagem no provedor, no momento em que ela foi lida.</summary>
    public string ExternalMessageId { get; private set; } = string.Empty;

    /// <summary>
    /// O <c>Message-ID</c> do cabeçalho — a única chave que sobrevive a mudança de pasta, e por
    /// isso a que a recaptura usa.
    /// </summary>
    public string? InternetMessageId { get; private set; }

    public string Sender { get; private set; } = string.Empty;
    public string? Subject { get; private set; }

    /// <summary>Quando o e-mail chegou à caixa.</summary>
    public DateTime ReceivedAt { get; private set; }

    /// <summary>Quando a varredura o encontrou.</summary>
    public DateTime FirstSeenAt { get; private set; }

    /// <summary>Quando o último anexo teve seu desfecho decidido. Nulo enquanto está na fila.</summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Onde o corpo do e-mail está guardado, cifrado, no balde — nulo para mensagem registrada
    /// antes de o corpo ser retido, ou quando o download falhou na sincronização.
    /// </summary>
    /// <remarks>
    /// O corpo é a exceção deliberada ao "metadado, nunca bytes" desta classe: ele é o que a
    /// tela de e-mail mostra e o que a extração por IA lê — e, como o anexo, carrega instrumento
    /// de pagamento, então sai por API apenas sob o mesmo portão do documento original (ADR-008).
    /// </remarks>
    public string? BodyStorageKey { get; private set; }

    /// <summary><c>text/html</c> ou <c>text/plain</c>, como o provedor declarou.</summary>
    public string? BodyContentType { get; private set; }

    /// <summary>Se há corpo guardado para servir — a resposta que a tela precisa sem abrir o balde.</summary>
    public bool HasStoredBody => !string.IsNullOrEmpty(BodyStorageKey);

    private readonly List<MessageArtifact> _artifacts = [];
    public IReadOnlyCollection<MessageArtifact> Artifacts => _artifacts.AsReadOnly();

    /// <summary>Quantos anexos o e-mail trazia — o número que a tela mostra.</summary>
    public int ArtifactCount => _artifacts.Count;

    /// <summary>
    /// Se algum anexo virou boleto. É o que torna o registro <strong>inpurgável</strong>: trilha
    /// de auditoria de um pagamento não expira com a janela de retenção.
    /// </summary>
    public bool ProducedBill => _artifacts.Exists(a => a.Outcome.ProducesBill);

    private CapturedMessage() { }

    private CapturedMessage(CapturedMessageId id) : base(id) { }

    /// <summary>Registra o e-mail lido, antes de qualquer processamento dos anexos.</summary>
    public static CapturedMessage Register(
        TenantId tenantId,
        CaptureSourceId sourceId,
        string externalMessageId,
        string sender,
        string? subject,
        DateTime receivedAt,
        DateTime occurredAt,
        IEnumerable<(string Key, string? FileName, string? ContentType)> artifacts,
        string? internetMessageId = null)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        if (sourceId.Equals(CaptureSourceId.Empty))
            throw CapturedMessageErrors.SourceRequired();

        var message = new CapturedMessage(CapturedMessageId.New())
        {
            TenantId = tenantId,
            SourceId = sourceId,
            ReceivedAt = receivedAt,
            FirstSeenAt = occurredAt,
        };

        message.SetExternalMessageId(externalMessageId);
        message.SetInternetMessageId(internetMessageId);
        message.SetSender(sender);
        message.SetSubject(subject);

        foreach (var (key, fileName, contentType) in artifacts)
        {
            // Chave repetida faria o desfecho de um anexo sobrescrever o do irmão, e o histórico
            // passaria a mentir sobre um dos dois.
            if (message._artifacts.Exists(a => string.Equals(a.ArtifactKey, key?.Trim(), StringComparison.Ordinal)))
                throw CapturedMessageErrors.DuplicateArtifactKey(key!);

            message._artifacts.Add(new MessageArtifact(key, fileName, contentType));
        }

        message.CreatedAt = occurredAt;
        message.UpdatedAt = occurredAt;
        return message;
    }

    /// <summary>Registra o que a captura decidiu sobre um anexo.</summary>
    /// <remarks>
    /// <c>ProcessedAt</c> anda a cada decisão em vez de ser carimbado no fim: um e-mail com três
    /// anexos é processado em três passagens do worker, e esperar a última deixaria a tela
    /// dizendo "não processado" sobre um e-mail já resolvido pela metade.
    /// </remarks>
    public void RecordOutcome(
        string artifactKey,
        ArtifactOutcome outcome,
        string? reason,
        CaptureItemId? captureItemId,
        BillId? billId,
        DateTime occurredAt)
    {
        var trimmed = artifactKey?.Trim();
        var artifact = _artifacts.Find(a => string.Equals(a.ArtifactKey, trimmed, StringComparison.Ordinal))
            ?? throw CapturedMessageErrors.ArtifactNotRegistered(artifactKey ?? string.Empty);

        artifact.Decide(outcome, reason, captureItemId, billId, occurredAt);

        ProcessedAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    /// <summary>Registra onde o corpo do e-mail ficou guardado.</summary>
    /// <remarks>
    /// Idempotente por substituição: uma recaptura que rebaixou o corpo grava o retrato mais
    /// recente. Corpo nunca é apagado por aqui — quem decide apagar é a purga da retenção, que
    /// remove o registro inteiro.
    /// </remarks>
    public void RecordBody(string storageKey, string contentType, DateTime occurredAt)
    {
        var trimmedKey = storageKey?.Trim();
        if (string.IsNullOrEmpty(trimmedKey))
            throw CapturedMessageErrors.BodyStorageKeyRequired();
        if (trimmedKey.Length > BODY_STORAGE_KEY_MAX_LENGTH)
            throw CapturedMessageErrors.TextTooLong(nameof(BodyStorageKey), BODY_STORAGE_KEY_MAX_LENGTH);

        var trimmedType = contentType?.Trim();
        if (string.IsNullOrEmpty(trimmedType))
            throw CapturedMessageErrors.BodyContentTypeRequired();
        if (trimmedType.Length > BODY_CONTENT_TYPE_MAX_LENGTH)
            throw CapturedMessageErrors.TextTooLong(nameof(BodyContentType), BODY_CONTENT_TYPE_MAX_LENGTH);

        BodyStorageKey = trimmedKey;
        BodyContentType = trimmedType;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Se a recaptura consegue reencontrar esta mensagem.
    /// </summary>
    /// <remarks>
    /// Sem o <c>Message-ID</c> do cabeçalho, a única chave é o id de armazenamento — que é
    /// exatamente o que morre quando alguém move a mensagem, e o motivo de a recaptura existir.
    /// </remarks>
    /// <summary>
    /// O e-mail foi puxado de novo do provedor e vai passar pela triagem inteira outra vez.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>O registro é o mesmo — mesmo id, mesma URL na tela.</strong> Até 2026-08-28 a
    /// recaptura apagava o registro e criava outro, e devolvia o id antigo na resposta: a tela
    /// recebia um id que não existia mais. Agora o registro é reescrito em cima do que existe.
    /// </para>
    /// <para>
    /// Os anexos são sincronizados com o que o provedor devolve <em>agora</em>: os que continuam
    /// existindo voltam a <c>Pending</c>, os que sumiram saem, os novos entram. O corpo guardado
    /// é esquecido (a chave antiga é devolvida para quem chamou apagar o blob DEPOIS do commit —
    /// apagar antes deixaria o registro apontando para nada se a transação não fechasse).
    /// </para>
    /// <para>
    /// Quem decide se a recaptura PODE acontecer é o <c>MessageRecaptureService</c>: a regra
    /// cruza este agregado com os itens e com os boletos, e por isso não mora aqui.
    /// </para>
    /// </remarks>
    /// <returns>A chave do corpo que estava guardado, ou <c>null</c> se não havia corpo.</returns>
    public string? Recapture(
        string externalMessageId,
        string sender,
        string? subject,
        DateTime receivedAt,
        IEnumerable<(string Key, string? FileName, string? ContentType)> artifacts,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(artifacts);
        EnsureCanBeRecaptured();

        SetExternalMessageId(externalMessageId);
        SetSender(sender);
        SetSubject(subject);
        ReceivedAt = receivedAt;
        ProcessedAt = null;

        var incoming = artifacts.ToList();
        var incomingKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (key, fileName, contentType) in incoming)
        {
            var trimmed = key?.Trim() ?? string.Empty;
            if (!incomingKeys.Add(trimmed))
                throw CapturedMessageErrors.DuplicateArtifactKey(key!);

            var existing = _artifacts.Find(a => string.Equals(a.ArtifactKey, trimmed, StringComparison.Ordinal));
            if (existing is null)
                _artifacts.Add(new MessageArtifact(trimmed, fileName, contentType));
            else
                existing.Reset(fileName, contentType);
        }

        _artifacts.RemoveAll(a => !incomingKeys.Contains(a.ArtifactKey));

        var previousBodyKey = BodyStorageKey;
        BodyStorageKey = null;
        BodyContentType = null;

        UpdatedAt = occurredAt;
        return previousBodyKey;
    }

    public bool CanBeRecaptured => !string.IsNullOrEmpty(InternetMessageId);

    /// <summary>Recusa a recaptura quando não há chave permanente para reencontrar a mensagem.</summary>
    public void EnsureCanBeRecaptured()
    {
        if (!CanBeRecaptured)
            throw CapturedMessageErrors.CannotRecaptureWithoutInternetMessageId(Id.Value);
    }

    private void SetExternalMessageId(string value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw CapturedMessageErrors.ExternalMessageIdRequired();
        if (trimmed.Length > EXTERNAL_MESSAGE_ID_MAX_LENGTH)
            throw CapturedMessageErrors.TextTooLong(nameof(ExternalMessageId), EXTERNAL_MESSAGE_ID_MAX_LENGTH);

        ExternalMessageId = trimmed;
    }

    private void SetInternetMessageId(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            InternetMessageId = null;
            return;
        }

        if (trimmed.Length > INTERNET_MESSAGE_ID_MAX_LENGTH)
            throw CapturedMessageErrors.TextTooLong(nameof(InternetMessageId), INTERNET_MESSAGE_ID_MAX_LENGTH);

        InternetMessageId = trimmed;
    }

    private void SetSender(string value)
    {
        var normalized = EmailSyntax.Normalize(value);
        if (normalized.Length > SENDER_MAX_LENGTH)
            throw CapturedMessageErrors.TextTooLong(nameof(Sender), SENDER_MAX_LENGTH);

        Sender = normalized;
    }

    private void SetSubject(string? value)
    {
        var trimmed = value?.Trim();
        Subject = string.IsNullOrEmpty(trimmed)
            ? null
            : trimmed[..Math.Min(trimmed.Length, SUBJECT_MAX_LENGTH)];
    }
}
