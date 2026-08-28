namespace BillPayment.Domain.CapturedMessages;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — Aggregate: CMS (CapturedMessage)
// Public porque a Application lança NotFound e as pré-condições da recaptura.
public static class CapturedMessageErrors
{
    private const string AGGREGATE_PREFIX = "BLP.CMS";

    public static DomainException NotFound(
        Guid capturedMessageId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}01",
            messageTemplate: "Registro de e-mail {0} não encontrado.",
            parameters: new object[] { capturedMessageId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    public static DomainException SourceRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}02",
            messageTemplate: "O registro de e-mail exige a fonte de captura que o trouxe.",
            parameters: [],
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Validation);

    public static DomainException ExternalMessageIdRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}03",
            messageTemplate: "O registro de e-mail exige o identificador da mensagem no provedor.",
            parameters: [],
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Validation);

    public static DomainException TextTooLong(
        string field,
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}04",
            messageTemplate: "O campo {0} do registro de e-mail excede {1} caracteres.",
            parameters: new object[] { field, maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Validation);

    public static DomainException ArtifactKeyRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}05",
            messageTemplate: "Cada anexo do registro de e-mail exige uma chave.",
            parameters: [],
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Validation);

    public static DomainException DuplicateArtifactKey(
        string artifactKey,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}06",
            messageTemplate: "O anexo '{0}' aparece duas vezes no mesmo registro de e-mail.",
            parameters: new object[] { artifactKey },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Validation);

    /// <summary>
    /// O desfecho chegou para um anexo que este registro não conhece. Sinal de que a ingestão e o
    /// processamento discordam sobre quais artefatos a mensagem tem — nunca deve acontecer, e
    /// falhar alto é melhor que gravar um histórico incompleto em silêncio.
    /// </summary>
    public static DomainException ArtifactNotRegistered(
        string artifactKey,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}07",
            messageTemplate: "O anexo '{0}' não pertence a este registro de e-mail.",
            parameters: new object[] { artifactKey },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// A mensagem não guarda o <c>Message-ID</c> do cabeçalho, e sem ele a recaptura não tem como
    /// reencontrá-la se o endereço de armazenamento tiver mudado.
    /// </summary>
    public static DomainException CannotRecaptureWithoutInternetMessageId(
        Guid capturedMessageId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}08",
            messageTemplate: "O e-mail {0} foi capturado antes de o sistema guardar o identificador "
                + "permanente da mensagem, e por isso não pode ser reprocessado do zero.",
            parameters: new object[] { capturedMessageId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException BodyStorageKeyRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}09",
            messageTemplate: "Registrar o corpo do e-mail exige a chave de armazenamento.",
            parameters: [],
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Validation);

    /// <summary>
    /// Um anexo deste e-mail virou boleto e alguém já autorizou o pagamento: refazer a triagem
    /// apagaria e recriaria o boleto por trás de quem decidiu. Mensagem sem o status — quem lê é
    /// o próprio tenant, e o id basta para a tela apontar o boleto.
    /// </summary>
    public static DomainException RecaptureBlockedByDecidedBill(
        Guid billId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}11",
            messageTemplate: "Este e-mail não pode ser recapturado: o boleto {0} já foi aprovado, agendado ou pago.",
            parameters: new object[] { billId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// O provedor não devolveu mais a mensagem pelo identificador do cabeçalho — apagada, movida
    /// para fora do alcance do aplicativo, ou caixa inacessível. Nada foi alterado.
    /// </summary>
    public static DomainException RecaptureSourceMessageNotFound(
        Guid capturedMessageId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}12",
            messageTemplate: "O e-mail {0} não foi encontrado na caixa para ser recapturado. Nada foi alterado.",
            parameters: new object[] { capturedMessageId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    public static DomainException BodyContentTypeRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}10",
            messageTemplate: "Registrar o corpo do e-mail exige o tipo de conteúdo.",
            parameters: [],
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Validation);

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
