namespace EconomicCore.Domain.SeedWork;

using System.IO;
using System.Runtime.CompilerServices;

// SeedWork (SWK) — erros transversais lançados pelas classes-base
public static class SeedWorkErrors
{
    private const string BC_PREFIX = "SWK";

    public static DomainException EmptyId(
        string entityTypeName,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{BC_PREFIX}01",
            messageTemplate: "Id de {0} não pode ser Guid.Empty.",
            parameters: new object[] { entityTypeName },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
