namespace BillPayment.Domain.CapturedMessages;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.SeedWork;

/// <summary>
/// Um anexo da mensagem, e o que a captura decidiu sobre ele.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Guarda metadado, nunca bytes.</strong> A retenção por desfecho continua valendo: o
/// arquivo só existe no balde quando o artefato reconheceu boleto, e este registro não muda isso
/// — ele existe para que uma pessoa saiba o que houve, não para reter documento.
/// </para>
/// <para>
/// <see cref="CaptureItemId"/> é nulo quando o desfecho foi <c>Discarded</c>: o item foi apagado
/// e não há para onde navegar. É a diferença entre "não achei" e "não existe mais".
/// </para>
/// <para>
/// Entidade interna: só o <see cref="CapturedMessage"/> cria e muta. Não emite Domain Event.
/// </para>
/// </remarks>
public sealed class MessageArtifact : Entity<MessageArtifactId>
{
    public const int ARTIFACT_KEY_MAX_LENGTH = 512;
    public const int FILE_NAME_MAX_LENGTH = 255;
    public const int CONTENT_TYPE_MAX_LENGTH = 150;
    public const int REASON_MAX_LENGTH = 200;

    /// <summary>O que distingue este anexo dos irmãos da mesma mensagem.</summary>
    public string ArtifactKey { get; private set; } = string.Empty;

    public string? FileName { get; private set; }
    public string? ContentType { get; private set; }

    /// <summary>O que a captura decidiu. <c>Pending</c> enquanto o processamento não passou.</summary>
    public ArtifactOutcome Outcome { get; private set; } = default!;

    /// <summary>Motivo em código estável, quando o desfecho tem um.</summary>
    public string? Reason { get; private set; }

    /// <summary>O item da quarentena, quando ele ainda existe.</summary>
    public CaptureItemId? CaptureItemId { get; private set; }

    /// <summary>O boleto que este anexo virou.</summary>
    public BillId? BillId { get; private set; }

    public DateTime? DecidedAt { get; private set; }

    private MessageArtifact() { }

    internal MessageArtifact(string artifactKey, string? fileName, string? contentType)
        : base(MessageArtifactId.New())
    {
        var trimmedKey = artifactKey?.Trim();
        if (string.IsNullOrEmpty(trimmedKey))
            throw CapturedMessageErrors.ArtifactKeyRequired();
        if (trimmedKey.Length > ARTIFACT_KEY_MAX_LENGTH)
            throw CapturedMessageErrors.TextTooLong(nameof(ArtifactKey), ARTIFACT_KEY_MAX_LENGTH);

        ArtifactKey = trimmedKey;
        FileName = Normalize(fileName, FILE_NAME_MAX_LENGTH, nameof(FileName));
        ContentType = Normalize(contentType, CONTENT_TYPE_MAX_LENGTH, nameof(ContentType));
        Outcome = ArtifactOutcome.Pending;
    }

    /// <summary>Registra o desfecho. Idempotente por natureza: reprocessar sobrescreve.</summary>
    internal void Decide(
        ArtifactOutcome outcome,
        string? reason,
        CaptureItemId? captureItemId,
        BillId? billId,
        DateTime occurredAt)
    {
        Outcome = outcome ?? ArtifactOutcome.Pending;
        Reason = Normalize(reason, REASON_MAX_LENGTH, nameof(Reason));
        CaptureItemId = captureItemId;
        BillId = billId;
        DecidedAt = occurredAt;
    }

    private static string? Normalize(string? value, int maxLength, string field)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return null;

        return trimmed.Length > maxLength
            ? throw CapturedMessageErrors.TextTooLong(field, maxLength)
            : trimmed;
    }
}
