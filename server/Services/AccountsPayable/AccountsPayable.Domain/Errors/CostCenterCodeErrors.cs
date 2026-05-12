namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// VO: CCC (CostCenterCode) — alfanumérico + hífen/underscore (ex.: "OBRA-SYRAH")
internal static class CostCenterCodeErrors
{
    private const string PREFIX = "AP.CCC";

    public static DomainException Empty(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Código do centro de custo não pode ser vazio.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException TooShort(
        int minLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Código do centro de custo deve ter ao menos {0} caracteres.",
            parameters: new object[] { minLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException TooLong(
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Código do centro de custo não pode exceder {0} caracteres.",
            parameters: new object[] { maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidFormat(
        string raw,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "Código do centro de custo inválido: '{0}' (aceita apenas letras, dígitos, hífen e underscore).",
            parameters: new object[] { raw },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
