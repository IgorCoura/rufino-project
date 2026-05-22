namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// VO: DLN (DigitableLine — linha digitável do boleto, 47 dígitos)
internal static class DigitableLineErrors
{
    private const string PREFIX = "AP.DLN";

    public static DomainException Empty(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Linha digitável não pode ser vazia.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidLength(
        int actualLength,
        int expectedLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Linha digitável deve ter {1} dígitos; recebeu {0}.",
            parameters: new object[] { actualLength, expectedLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException NonNumeric(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Linha digitável deve conter apenas dígitos (espaços, pontos e hífen são tolerados como separadores).",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
