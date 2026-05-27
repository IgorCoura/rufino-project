namespace EconomicCore.Domain.Services;

using System.IO;
using System.Runtime.CompilerServices;
using EconomicCore.Domain.SeedWork;

internal static class DualityMatchingErrors
{
    private const string PREFIX = "ECC.DMS";

    public static DomainException NullEvent(
        string parameterName,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Argumento {0} não pode ser nulo no matching de duality.",
            parameters: new object[] { parameterName },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException TenantMismatch(
        Guid expectedTenant,
        Guid actualTenant,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Tenants distintos no matching: payment={0}, consumption={1}.",
            parameters: new object[] { expectedTenant, actualTenant },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException ConsumptionNotCoveredByCommitment(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "ConsumptionEvent não está coberto por nenhum Commitment.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException PaymentNotCoveredByCommitment(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "PaymentEvent não está coberto por nenhum Commitment.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException CurrencyMismatch(
        string expectedCurrency,
        string actualCurrency,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}05",
            messageTemplate: "Currencies distintas no matching: payment={0}, consumption={1}.",
            parameters: new object[] { expectedCurrency, actualCurrency },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
