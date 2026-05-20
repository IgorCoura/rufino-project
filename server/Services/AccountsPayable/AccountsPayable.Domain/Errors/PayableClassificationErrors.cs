namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// Domain Service: PCL (PayableClassificationValidator) — cross-aggregate validator
internal static class PayableClassificationErrors
{
    private const string PREFIX = "AP.PCL";

    public static DomainException AccountNotFound(
        Guid accountId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Conta contábil {0} não existe no plano de contas informado.",
            parameters: new object[] { accountId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AccountInactive(
        Guid accountId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Conta contábil {0} está inativa.",
            parameters: new object[] { accountId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AccountTypeNotAllowed(
        string actualType,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Tipo de conta {0} não é permitido para classificação de contas a pagar (esperado EXPENSE).",
            parameters: new object[] { actualType },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CostCenterInactive(
        Guid costCenterId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "Centro de custo {0} está inativo.",
            parameters: new object[] { costCenterId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException TenantMismatch(
        string resource,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}05",
            messageTemplate: "{0} pertence a outro tenant — operação não permitida.",
            parameters: new object[] { resource },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ChartMismatch(
        Guid expectedChartId,
        Guid actualChartId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}06",
            messageTemplate: "AccountRef aponta para o plano de contas {0}, mas o ChartOfAccounts fornecido é {1} — a âncora ao Aggregate Root precisa casar.",
            parameters: new object[] { expectedChartId, actualChartId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
