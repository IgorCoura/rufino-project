namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// VO: ADR (Address)
internal static class AddressErrors
{
    private const string PREFIX = "AP.ADR";

    public static DomainException FieldEmpty(
        string fieldName,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Campo '{0}' do endereço não pode ser vazio.",
            parameters: new object[] { fieldName },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException FieldTooLong(
        string fieldName,
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Campo '{0}' do endereço não pode exceder {1} caracteres.",
            parameters: new object[] { fieldName, maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidState(
        string state,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "UF '{0}' inválida (esperado 2 letras maiúsculas).",
            parameters: new object[] { state },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidZipCode(
        string zipCode,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "CEP '{0}' inválido (esperado 8 dígitos).",
            parameters: new object[] { zipCode },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
