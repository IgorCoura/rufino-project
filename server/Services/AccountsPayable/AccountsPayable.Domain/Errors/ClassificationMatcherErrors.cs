namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// VO: CMR (ClassificationMatcher)
internal static class ClassificationMatcherErrors
{
    private const string PREFIX = "AP.CMR";

    public static DomainException AtLeastOneCriterionRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Matcher de classificação exige pelo menos um critério (fornecedor, palavra-chave ou faixa de valor).",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidValueRange(
        decimal min,
        decimal max,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Faixa de valor inválida: mínimo {0} maior que máximo {1}.",
            parameters: new object[] { min, max },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException KeywordTooLong(
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Palavra-chave excede o limite de {0} caracteres.",
            parameters: new object[] { maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
