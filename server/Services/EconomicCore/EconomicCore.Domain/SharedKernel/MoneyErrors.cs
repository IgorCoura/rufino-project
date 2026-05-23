namespace EconomicCore.Domain.SharedKernel;

using System.IO;
using System.Runtime.CompilerServices;
using EconomicCore.Domain.SeedWork;

internal static class MoneyErrors
{
    private const string PREFIX = "SHK.MNY";

    public static DomainException CurrencyRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Currency é obrigatória para criar Money.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CurrencyMismatch(
        string expected,
        string actual,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Operação inválida entre moedas diferentes: esperado {0}, recebido {1}.",
            parameters: new object[] { expected, actual },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
