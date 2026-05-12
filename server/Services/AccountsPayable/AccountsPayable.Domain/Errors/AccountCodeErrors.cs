namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// VO: ACO (AccountCode) — formato hierárquico de dígitos separados por ponto (ex.: "4.01.01")
internal static class AccountCodeErrors
{
    private const string PREFIX = "AP.ACO";

    public static DomainException Empty(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Código da conta contábil não pode ser vazio.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidFormat(
        string raw,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Código da conta contábil inválido: '{0}' (esperado dígitos separados por ponto, ex.: 4.01.01).",
            parameters: new object[] { raw },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException TooLong(
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Código da conta contábil não pode exceder {0} caracteres.",
            parameters: new object[] { maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
