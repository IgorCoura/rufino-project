namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// Aggregate: AAP (AutoApprovalPolicy) + internal entity ARU (ApprovalRule)
internal static class AutoApprovalPolicyErrors
{
    private const string PREFIX = "AP.AAP";

    public static DomainException RequiredApprovalCountTooLow(
        int count,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Quantidade requerida de aprovações deve ser ≥ 1; recebido: {0}.",
            parameters: new object[] { count },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ApprovalRuleNotFound(
        Guid ruleId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Regra de aprovação {0} não encontrada na política.",
            parameters: new object[] { ruleId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ApprovalRuleAlreadyActive(
        Guid ruleId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Regra de aprovação {0} já está ativa.",
            parameters: new object[] { ruleId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ApprovalRuleAlreadyInactive(
        Guid ruleId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "Regra de aprovação {0} já está inativa.",
            parameters: new object[] { ruleId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
