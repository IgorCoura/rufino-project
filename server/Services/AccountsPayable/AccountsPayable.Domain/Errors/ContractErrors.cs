namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// Aggregate: CTR (Contract)
internal static class ContractErrors
{
    private const string PREFIX = "AP.CTR";

    public static DomainException InvalidStatusTransition(
        string currentStatus,
        string targetStatus,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Transição inválida de status do contrato: {0} → {1}.",
            parameters: new object[] { currentStatus, targetStatus },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException PaymentDayOutOfRange(
        int paymentDay,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Dia de pagamento {0} fora do intervalo [1..31].",
            parameters: new object[] { paymentDay },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException EndDateBeforeStartDate(
        DateOnly start,
        DateOnly end,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Data fim {1} anterior à data início {0}.",
            parameters: new object[] { start, end },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException TerminationReasonRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "Motivo é obrigatório para terminar o contrato.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException SuspensionReasonRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}05",
            messageTemplate: "Motivo é obrigatório para suspender o contrato.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AmountUnchanged(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}06",
            messageTemplate: "Novo valor mensal igual ao atual — sem mudança a registrar.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CurrencyCannotChange(
        string currentCurrency,
        string newCurrency,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}07",
            messageTemplate: "Moeda do contrato ({0}) não pode mudar entre revisões; recebida {1}.",
            parameters: new object[] { currentCurrency, newCurrency },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
