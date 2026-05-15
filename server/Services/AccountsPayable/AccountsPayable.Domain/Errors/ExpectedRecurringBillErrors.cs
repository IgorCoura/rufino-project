namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// Aggregate: ERB (ExpectedRecurringBill)
internal static class ExpectedRecurringBillErrors
{
    private const string PREFIX = "AP.ERB";

    public static DomainException NotPending(
        string currentStatus,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Conta esperada em status {0} não admite mais transições — só Pending suporta match/missed/cancel.",
            parameters: new object[] { currentStatus },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CancellationReasonRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Motivo é obrigatório para cancelar a conta esperada.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
