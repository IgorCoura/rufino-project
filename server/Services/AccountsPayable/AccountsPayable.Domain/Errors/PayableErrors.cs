namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// Aggregate: PAY (Payable) — Event-Sourced (D-405)
internal static class PayableErrors
{
    private const string PREFIX = "AP.PAY";

    public static DomainException InvalidStatusTransition(
        string currentStatus,
        string targetStatus,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Transição inválida de status da conta a pagar: {0} → {1}.",
            parameters: new object[] { currentStatus, targetStatus },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException DueDateInPast(
        DateOnly attemptedDueDate,
        DateOnly today,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Vencimento {0} é anterior à data de criação {1}.",
            parameters: new object[] { attemptedDueDate, today },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ReasonRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Motivo é obrigatório para cancelar a conta a pagar.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CannotPayCancelled(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "Conta a pagar já cancelada não pode ser marcada como paga.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CannotScheduleWithoutClassification(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}05",
            messageTemplate: "Conta a pagar deve ser classificada antes de ser agendada.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CannotClassifyTerminalPayable(
        string currentStatus,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}06",
            messageTemplate: "Conta a pagar em status {0} não pode ser (re)classificada.",
            parameters: new object[] { currentStatus },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
