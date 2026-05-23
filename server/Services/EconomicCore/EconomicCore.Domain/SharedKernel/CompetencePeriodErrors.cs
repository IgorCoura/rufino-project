namespace EconomicCore.Domain.SharedKernel;

using System.IO;
using System.Runtime.CompilerServices;
using EconomicCore.Domain.SeedWork;

internal static class CompetencePeriodErrors
{
    private const string PREFIX = "SHK.PER";

    public static DomainException InvalidYear(
        int year,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Ano de competência inválido: {0} (esperado entre {1} e {2}).",
            parameters: new object[] { year, CompetencePeriod.MIN_YEAR, CompetencePeriod.MAX_YEAR },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidMonth(
        int month,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Mês de competência inválido: {0} (esperado entre {1} e {2}).",
            parameters: new object[] { month, CompetencePeriod.MIN_MONTH, CompetencePeriod.MAX_MONTH },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
