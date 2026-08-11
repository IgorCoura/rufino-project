namespace BillPayment.Domain.CaptureSources;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — Aggregate: CPS (CaptureSource)
// Public porque a Application precisa lançar NotFound e AlreadyConnected nas pré-condições
// de orquestração (ver "Regras invioláveis de Handler" no CLAUDE.md).
public static class CaptureSourceErrors
{
    private const string AGGREGATE_PREFIX = "BLP.CPS";

    /// <summary>
    /// A invariante central do Aggregate: o Domain guarda o ponteiro, nunca o segredo. Uma fonte
    /// sem referência de credencial seria uma fonte que ninguém consegue sincronizar.
    /// </summary>
    public static DomainException CredentialRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}01",
            messageTemplate: "A fonte de captura precisa apontar para uma credencial no cofre.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException NotFound(
        Guid captureSourceId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}02",
            messageTemplate: "Fonte de captura {0} não encontrada.",
            parameters: new object[] { captureSourceId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    public static DomainException KindRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}03",
            messageTemplate: "O tipo da fonte de captura é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AddressRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}04",
            messageTemplate: "O endereço da fonte de captura é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AddressTooLong(
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}05",
            messageTemplate: "O endereço da fonte excede o limite de {0} caracteres.",
            parameters: new object[] { maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidMailboxAddress(
        string value,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}06",
            messageTemplate: "'{0}' não é um endereço de caixa de e-mail válido.",
            parameters: new object[] { value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>Portal só é monitorado por <c>https</c> — ver os controles de egresso do doc 09.</summary>
    public static DomainException InvalidPortalUrl(
        string value,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}07",
            messageTemplate: "'{0}' não é uma URL https válida de portal.",
            parameters: new object[] { value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException DisplayNameRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}08",
            messageTemplate: "O nome de exibição da fonte é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException DisplayNameTooLong(
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}09",
            messageTemplate: "O nome de exibição da fonte excede o limite de {0} caracteres.",
            parameters: new object[] { maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Conflito <strong>dentro do tenant</strong>. A colisão com outro tenant não é erro — é o
    /// caso de fonte compartilhada do ADR-008, que devolve aviso genérico, não exceção.
    /// </summary>
    public static DomainException AlreadyConnected(
        string address,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}10",
            messageTemplate: "A fonte '{0}' já está conectada nesta conta.",
            parameters: new object[] { address },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException SyncCursorTooLong(
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}11",
            messageTemplate: "O cursor de sincronização excede o limite de {0} caracteres.",
            parameters: new object[] { maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Desligar a fonte é o botão de parada do usuário. Sincronizar assim mesmo tornaria o
    /// botão decorativo — e a fonte desligada é justamente a que pode ter credencial revogada.
    /// </summary>
    public static DomainException CannotSyncDisabled(
        Guid captureSourceId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}12",
            messageTemplate: "A fonte de captura {0} está desativada e não pode ser sincronizada.",
            parameters: new object[] { captureSourceId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// A credencial não alcança a caixa. É a prova de acesso do ADR-006 falhando — e ela roda
    /// <strong>antes</strong> de a fonte existir, senão o sistema guardaria uma fonte que nunca
    /// vai sincronizar e o usuário só descobriria pela ausência silenciosa de boletos.
    /// </summary>
    public static DomainException MailboxAccessDenied(
        string address,
        string reasonCode,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}13",
            messageTemplate: "O acesso à caixa '{0}' foi recusado pelo provedor ({1}).",
            parameters: new object[] { address, reasonCode },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// O provedor não respondeu. Distinto de <see cref="MailboxAccessDenied"/> de propósito:
    /// aqui nada se aprendeu sobre a credencial, e tentar de novo mais tarde é o certo.
    /// </summary>
    public static DomainException MailboxUnreachable(
        string address,
        string reasonCode,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}14",
            messageTemplate: "Não foi possível falar com o provedor da caixa '{0}' agora ({1}). Tente novamente.",
            parameters: new object[] { address, reasonCode },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException FolderPathTooLong(
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}15",
            messageTemplate: "O caminho da pasta monitorada excede o limite de {0} caracteres.",
            parameters: new object[] { maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException FolderAlreadyMonitored(
        string folderPath,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}16",
            messageTemplate: "A pasta '{0}' já é acompanhada por esta fonte.",
            parameters: new object[] { folderPath },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException FolderNotMonitored(
        string folderPath,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}17",
            messageTemplate: "A pasta '{0}' não é acompanhada por esta fonte.",
            parameters: new object[] { folderPath },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    /// <summary>
    /// Uma fonte sem pasta nenhuma não varreria nada — e não avisaria, porque zero pasta produz
    /// zero item exatamente como uma caixa vazia. Desligar a captura é o que existe para isso.
    /// </summary>
    public static DomainException CannotRemoveLastFolder(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}18",
            messageTemplate: "A fonte precisa acompanhar ao menos uma pasta. Para parar de varrer, desative a fonte.",
            parameters: [],
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Cada pasta acompanhada é uma chamada ao provedor por ciclo de varredura; sem teto, um
    /// cadastro distraído vira limitação de taxa na caixa inteira.
    /// </summary>
    public static DomainException TooManyFolders(
        int maxFolders,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}19",
            messageTemplate: "Uma fonte pode acompanhar no máximo {0} pastas.",
            parameters: new object[] { maxFolders },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
