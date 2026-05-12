namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// VO: CON (ChartOfAccountsName)
internal static class ChartOfAccountsNameErrors
{
    private const string PREFIX = "AP.CON";

    public static DomainException Empty(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Nome do plano de contas não pode ser vazio.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException TooShort(
        int minLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Nome do plano de contas deve ter ao menos {0} caracteres.",
            parameters: new object[] { minLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException TooLong(
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Nome do plano de contas não pode exceder {0} caracteres.",
            parameters: new object[] { maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
