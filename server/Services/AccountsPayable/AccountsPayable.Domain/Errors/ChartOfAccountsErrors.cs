namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// Aggregate: COA (ChartOfAccounts) — também cobre a Entity interna Account
internal static class ChartOfAccountsErrors
{
    private const string PREFIX = "AP.COA";

    public static DomainException AccountTypeRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Tipo da conta contábil é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException DuplicatedAccountCode(
        string code,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Código '{0}' já existe no plano de contas.",
            parameters: new object[] { code },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ParentNotFound(
        Guid parentId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Conta-pai {0} não existe neste plano de contas.",
            parameters: new object[] { parentId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ParentInactive(
        Guid parentId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "Conta-pai {0} está inativa — não pode receber novos filhos.",
            parameters: new object[] { parentId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException MaxDepthExceeded(
        int maxDepth,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}05",
            messageTemplate: "Profundidade máxima do plano de contas ({0} níveis) excedida.",
            parameters: new object[] { maxDepth },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AccountNotFound(
        Guid accountId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}06",
            messageTemplate: "Conta {0} não existe neste plano de contas.",
            parameters: new object[] { accountId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AccountAlreadyInactive(
        Guid accountId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}07",
            messageTemplate: "Conta {0} já está inativa.",
            parameters: new object[] { accountId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CannotDeactivateAccountWithActiveChildren(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}08",
            messageTemplate: "Conta não pode ser desativada enquanto tiver filhos ativos. Desative os filhos primeiro.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
