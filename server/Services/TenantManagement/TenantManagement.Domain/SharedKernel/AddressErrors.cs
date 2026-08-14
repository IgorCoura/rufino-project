namespace TenantManagement.Domain.SharedKernel;

using System.IO;
using System.Runtime.CompilerServices;
using TenantManagement.Domain.SeedWork;

// BC: TNM (TenantManagement) — VO: ADR (Address)
internal static class AddressErrors
{
    private const string PREFIX = "TNM.ADR";

    public static DomainException InvalidZipCode(
        string value,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "CEP inválido: {0}. Informe os 8 dígitos.",
            parameters: new object[] { value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException FieldRequired(
        string fieldName,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "O campo {0} do endereço é obrigatório.",
            parameters: new object[] { fieldName },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException FieldTooLong(
        string fieldName,
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "O campo {0} do endereço excede o limite de {1} caracteres.",
            parameters: new object[] { fieldName, maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidState(
        string value,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "UF inválida: {0}. Informe a sigla de duas letras.",
            parameters: new object[] { value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
