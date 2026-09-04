namespace BillPayment.Domain.Instruments;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — VO: DGL (DigitableLine)
// Public porque a Application recusa importação de documento ilegível na borda.
public static class DigitableLineErrors
{
    private const string PREFIX = "BLP.DGL";

    public static DomainException Required(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "A linha digitável é obrigatória.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Quantidade de dígitos que não corresponde a nenhum dos dois layouts. A mensagem não
    /// repete a linha inteira: ela vai para tela e para log, e linha digitável completa é
    /// instrumento de pagamento.
    /// </summary>
    public static DomainException InvalidLength(
        int actualLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "A linha digitável precisa ter 47 dígitos (cobrança) ou 48 (arrecadação). Recebidos: {0}.",
            parameters: new object[] { actualLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidFieldCheckDigit(
        int fieldNumber,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "O dígito verificador do campo {0} da linha digitável não confere.",
            parameters: new object[] { fieldNumber },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>O DV geral cobre o código de barras inteiro — inclusive o código do banco.</summary>
    public static DomainException InvalidGeneralCheckDigit(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "O dígito verificador geral do código de barras não confere.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>Banco "000" não existe na tabela COMPE e denuncia código de barras fabricado.</summary>
    public static DomainException UnassignedBank(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}05",
            messageTemplate: "O código de barras não indica um banco liquidante válido.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Pedir o banco de uma conta de convênio é erro de programação, não de dado: o layout
    /// de arrecadação não tem esse campo. Existe para a chamada indevida falhar alto.
    /// </summary>
    public static DomainException BankCodeNotAvailable(
        string billKind,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}06",
            messageTemplate: "Documento do tipo {0} não carrega código de banco. Consulte CarriesBankCode antes.",
            parameters: new object[] { billKind },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
