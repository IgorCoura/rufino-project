namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// VO: AMC (ApprovalMatchCriteria)
internal static class ApprovalMatchCriteriaErrors
{
    private const string PREFIX = "AP.AMC";

    public static DomainException AtLeastOneCriterionRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Critério de aprovação exige pelo menos um filtro (fornecedor, conta contábil ou faixa de valor).",
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

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
