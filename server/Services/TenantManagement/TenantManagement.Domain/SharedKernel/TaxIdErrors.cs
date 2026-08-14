namespace TenantManagement.Domain.SharedKernel;

using System.IO;
using System.Runtime.CompilerServices;
using TenantManagement.Domain.SeedWork;

// BC: TNM (TenantManagement) — VO: TAX (TaxId)
internal static class TaxIdErrors
{
    private const string PREFIX = "TNM.TAX";

    public static DomainException KindRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "O tipo do documento (CPF ou CNPJ) é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidFormat(
        string value,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Documento com formato inválido: {0}.",
            parameters: new object[] { value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidCheckDigit(
        string value,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Documento com dígito verificador inválido: {0}.",
            parameters: new object[] { value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
