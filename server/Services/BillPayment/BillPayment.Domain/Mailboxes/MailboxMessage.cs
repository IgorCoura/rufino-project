namespace BillPayment.Domain.Mailboxes;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Um artefato dentro de uma mensagem: um anexo, ou o próprio corpo quando ele carrega o boleto.
/// </summary>
/// <remarks>
/// <strong>Só metadado, nunca os bytes.</strong> A delta query do provedor devolve a listagem;
/// baixar o conteúdo é chamada à parte e acontece na sprint 2.3, junto com o armazenamento
/// cifrado. Carregar o binário aqui faria uma varredura de caixa trazer megabytes que talvez
/// nem sejam boleto.
/// </remarks>
public sealed class MailboxArtifact : ValueObject
{
    public const int KEY_MAX_LENGTH = 512;
    public const int FILE_NAME_MAX_LENGTH = 255;
    public const int CONTENT_TYPE_MAX_LENGTH = 150;

    /// <summary>O que distingue este artefato dos irmãos da mesma mensagem — vira o <c>ArtifactKey</c>.</summary>
    public string Key { get; }

    public string? FileName { get; }
    public string? ContentType { get; }
    public long SizeInBytes { get; }

    private MailboxArtifact(string key, string? fileName, string? contentType, long sizeInBytes)
    {
        Key = key;
        FileName = fileName;
        ContentType = contentType;
        SizeInBytes = sizeInBytes;
    }

    public static MailboxArtifact From(string key, string? fileName, string? contentType, long sizeInBytes)
    {
        var trimmedKey = key?.Trim();
        if (string.IsNullOrEmpty(trimmedKey))
            throw MailboxErrors.ArtifactKeyRequired();

        return new MailboxArtifact(
            Clamp(trimmedKey, KEY_MAX_LENGTH),
            Normalize(fileName, FILE_NAME_MAX_LENGTH),
            Normalize(contentType, CONTENT_TYPE_MAX_LENGTH),
            Math.Max(0, sizeInBytes));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Key;
        yield return FileName;
        yield return ContentType;
        yield return SizeInBytes;
    }

    private static string? Normalize(string? value, int maxLength)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : Clamp(trimmed, maxLength);
    }

    private static string Clamp(string value, int maxLength)
        => value.Length > maxLength ? value[..maxLength] : value;
}

/// <summary>
/// Uma mensagem lida da caixa, com os artefatos que ela carrega.
/// </summary>
/// <remarks>
/// O remetente vem normalizado por <c>EmailSyntax</c> — é o mesmo endereço que o
/// <c>TrustedOrigin</c> vai tentar resolver, e normalizar em dois lugares diferentes é como a
/// resolução passa a divergir do que foi cadastrado.
/// </remarks>
public sealed class MailboxMessage : ValueObject
{
    public const int MESSAGE_ID_MAX_LENGTH = 512;
    public const int SUBJECT_MAX_LENGTH = 500;

    /// <summary>O <c>Message-ID</c> do cabeçalho RFC-822 cabe folgado em 512.</summary>
    public const int INTERNET_MESSAGE_ID_MAX_LENGTH = 512;

    public string MessageId { get; }

    /// <summary>
    /// O <c>Message-ID</c> do cabeçalho da mensagem — <strong>do e-mail, não da cópia</strong>.
    /// </summary>
    /// <remarks>
    /// <see cref="MessageId"/> é o endereço de onde o item está guardado, e a pasta faz parte
    /// dele: mover a mensagem o invalida. Este aqui é escrito pelo remetente e acompanha o
    /// e-mail para sempre, o que o torna a única chave capaz de reencontrar a mensagem depois
    /// que o id de armazenamento morre. Nulo quando o provedor não o informa.
    /// </remarks>
    public string? InternetMessageId { get; }
    public string Sender { get; }
    public string? Subject { get; }
    public DateTimeOffset ReceivedAt { get; }

    private readonly List<MailboxArtifact> _artifacts;
    public IReadOnlyList<MailboxArtifact> Artifacts => _artifacts.AsReadOnly();

    private MailboxMessage(
        string messageId,
        string? internetMessageId,
        string sender,
        string? subject,
        DateTimeOffset receivedAt,
        List<MailboxArtifact> artifacts)
    {
        MessageId = messageId;
        InternetMessageId = internetMessageId;
        Sender = sender;
        Subject = subject;
        ReceivedAt = receivedAt;
        _artifacts = artifacts;
    }

    public static MailboxMessage From(
        string messageId,
        string sender,
        string? subject,
        DateTimeOffset receivedAt,
        IEnumerable<MailboxArtifact> artifacts,
        string? internetMessageId = null)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        var trimmedId = messageId?.Trim();
        if (string.IsNullOrEmpty(trimmedId))
            throw MailboxErrors.MessageIdRequired();

        var list = artifacts.ToList();

        // Chave repetida colidiria no índice único da ingestão e o segundo boleto sumiria em
        // silêncio. Falhar aqui expõe o adapter mal escrito em vez de perder documento.
        var duplicada = list.GroupBy(a => a.Key, StringComparer.Ordinal)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicada is not null)
            throw MailboxErrors.DuplicateArtifactKey(duplicada.Key);

        var trimmedSubject = subject?.Trim();

        var trimmedInternetId = internetMessageId?.Trim();

        return new MailboxMessage(
            trimmedId.Length > MESSAGE_ID_MAX_LENGTH ? trimmedId[..MESSAGE_ID_MAX_LENGTH] : trimmedId,
            string.IsNullOrEmpty(trimmedInternetId)
                ? null
                : trimmedInternetId[..Math.Min(trimmedInternetId.Length, INTERNET_MESSAGE_ID_MAX_LENGTH)],
            EmailSyntax.Normalize(sender),
            string.IsNullOrEmpty(trimmedSubject)
                ? null
                : trimmedSubject[..Math.Min(trimmedSubject.Length, SUBJECT_MAX_LENGTH)],
            receivedAt,
            list);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MessageId;
        yield return InternetMessageId;
        yield return Sender;
        yield return Subject;
        yield return ReceivedAt;

        foreach (var artifact in _artifacts)
            yield return artifact;
    }
}
