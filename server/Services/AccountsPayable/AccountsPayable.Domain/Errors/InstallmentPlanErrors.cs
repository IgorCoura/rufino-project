namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// Aggregate: IPL (InstallmentPlan)
internal static class InstallmentPlanErrors
{
    private const string PREFIX = "AP.IPL";

    public static DomainException InstallmentCountTooLow(
        int count,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Parcelamento exige pelo menos 2 parcelas; recebido: {0}.",
            parameters: new object[] { count },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InstallmentNumberOutOfRange(
        int installmentNumber,
        int installmentCount,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Número da parcela {0} fora do intervalo [1..{1}].",
            parameters: new object[] { installmentNumber, installmentCount },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InstallmentNumberAlreadyRegistered(
        int installmentNumber,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Parcela {0} já registrada no plano.",
            parameters: new object[] { installmentNumber },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AlreadyCancelled(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "Plano de parcelamento já está cancelado.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CancellationReasonRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}05",
            messageTemplate: "Motivo é obrigatório para cancelar o plano de parcelamento.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CannotRegisterOnCancelled(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}06",
            messageTemplate: "Plano cancelado não aceita novos vínculos com Payable.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AmountSumMismatch(
        decimal expectedTotal,
        decimal actualSum,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}07",
            messageTemplate: "Soma das parcelas ({1}) diverge do total do plano ({0}) acima da tolerância de centavo.",
            parameters: new object[] { expectedTotal, actualSum },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
