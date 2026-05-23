namespace EconomicCore.Domain;

using System.IO;
using System.Runtime.CompilerServices;
using EconomicCore.Domain.SeedWork;

// BC: ECC (EconomicCore) — erros transversais (ex.: multi-tenant)
internal static class EconomicCoreErrors
{
    private const string BC_PREFIX = "ECC";

    public static DomainException TenantMismatch(
        string expectedTenantId,
        string actualTenantId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{BC_PREFIX}01",
            messageTemplate: "Operação não permitida: tenant {0} não pode acessar recurso de {1}.",
            parameters: new object[] { actualTenantId, expectedTenantId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
