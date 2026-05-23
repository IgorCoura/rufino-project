namespace EconomicCore.Domain.SharedKernel;

using System.IO;
using System.Runtime.CompilerServices;
using EconomicCore.Domain.SeedWork;

internal static class DateRangeErrors
{
    private const string PREFIX = "SHK.DRG";

    public static DomainException InvalidRange(
        DateOnly from,
        DateOnly to,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "DateRange inválido: From ({0}) não pode ser maior que To ({1}).",
            parameters: new object[] { from, to },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
