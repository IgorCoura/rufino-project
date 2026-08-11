namespace BillPayment.Domain.CaptureItems;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — Aggregate: CPI (CaptureItem)
// Public porque a Application precisa lançar NotFound e as pré-condições de reivindicação.
// BLP.CPI04 é fixado pelo doc 07 — não renumere.
public static class CaptureItemErrors
{
    private const string AGGREGATE_PREFIX = "BLP.CPI";

    public static DomainException NotFound(
        Guid captureItemId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}01",
            messageTemplate: "Item capturado {0} não encontrado.",
            parameters: new object[] { captureItemId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    /// <summary>
    /// A mesma mensagem/artefato já foi ingerida por esta fonte. Idempotência da ingestão por
    /// <c>(TenantId, SourceId, ExternalMessageId, ArtifactKey)</c> — reprocessar a caixa não
    /// pode gerar item novo.
    /// </summary>
    public static DomainException AlreadyIngested(
        string externalMessageId,
        string artifactKey,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}02",
            messageTemplate: "O artefato '{1}' da mensagem {0} já foi ingerido por esta fonte.",
            parameters: new object[] { externalMessageId, artifactKey },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException InvalidTransition(
        string from,
        string to,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}03",
            messageTemplate: "Um item capturado em '{0}' não pode passar para '{1}'.",
            parameters: new object[] { from, to },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// A escada de roteamento já tinha concluído que o pagador é outro. Reivindicar assim mesmo
    /// seria o usuário pagando a conta de terceiro — o desfecho que o doc 07 existe para impedir.
    /// </summary>
    public static DomainException ClaimContradictsExtractedPayer(
        Guid captureItemId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}04",
            messageTemplate: "O item {0} tem pagador identificado que não é desta conta e não pode ser reivindicado.",
            parameters: new object[] { captureItemId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException ExternalMessageIdRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}05",
            messageTemplate: "O identificador da mensagem no provedor é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ArtifactKeyRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}06",
            messageTemplate: "A chave do artefato dentro da mensagem é obrigatória.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException SourceRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}07",
            messageTemplate: "O item capturado precisa pertencer a uma fonte de captura.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException TextTooLong(
        string field,
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}08",
            messageTemplate: "O campo '{0}' do item capturado excede o limite de {1} caracteres.",
            parameters: new object[] { field, maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ArtifactRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}09",
            messageTemplate: "O item precisa ter o artefato armazenado antes de ser processado.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException BillRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}10",
            messageTemplate: "A promoção de um item capturado exige o boleto que ele gerou.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException RoutingConfidenceRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}11",
            messageTemplate: "A promoção de um item capturado exige registrar por qual degrau ele foi roteado.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ReasonRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}12",
            messageTemplate: "O motivo da quarentena é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ClaimedByRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}13",
            messageTemplate: "A reivindicação precisa ser atribuída a um usuário.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException SourceUrlRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}14",
            messageTemplate: "Um item que aguarda download precisa registrar a URL de origem.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
