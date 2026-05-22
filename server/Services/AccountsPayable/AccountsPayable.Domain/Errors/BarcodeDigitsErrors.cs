namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// VO: BCD (BarcodeDigits — boleto código de barras 44 dígitos)
internal static class BarcodeDigitsErrors
{
    private const string PREFIX = "AP.BCD";

    public static DomainException Empty(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Código de barras não pode ser vazio.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidLength(
        int actualLength,
        int expectedLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Código de barras deve ter {1} dígitos; recebeu {0}.",
            parameters: new object[] { actualLength, expectedLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException NonNumeric(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Código de barras deve conter apenas dígitos.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidDigitVerifier(
        int provided,
        int expected,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "Dígito verificador do código de barras inválido (mod-11): recebido {0}, esperado {1}.",
            parameters: new object[] { provided, expected },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidBankCode(
        string bankCode,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}05",
            messageTemplate: "Código de banco do boleto inválido: '{0}'.",
            parameters: new object[] { bankCode },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
